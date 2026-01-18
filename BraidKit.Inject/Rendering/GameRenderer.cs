using BraidKit.Core;
using BraidKit.Core.Game;
using BraidKit.Core.Helpers;
using BraidKit.Core.Network;
using System.Numerics;
using Vortice.Direct3D9;
using Vortice.Mathematics;
using Rect = Vortice.Mathematics.Rect;

namespace BraidKit.Inject.Rendering;

internal class GameRenderer(BraidGame _braidGame, IDirect3DDevice9 _device) : IDisposable
{
    private readonly LineRenderer _lineRenderer = new(_device);
    private readonly TextRenderer _textRenderer = new(_device);
    private readonly SimpleRenderer _simpleRenderer = new(_device);
    public RenderSettings RenderSettings { get; set; } = RenderSettings.Off;

    public void Dispose()
    {
        _lineRenderer.Dispose();
        _textRenderer.Dispose();
        _simpleRenderer.Dispose();
    }

    public void Render()
    {
        if (!RenderSettings.IsRenderingActive() || _braidGame.InMainMenu || _braidGame.InPuzzleAssemblyScreen)
            return;

        GetMatrices(out var viewProjMtx, out var screenMtx, out var worldToScreenMtx);

        if (RenderSettings.RenderEntityBounds || RenderSettings.RenderEntityCenters)
        {
            _lineRenderer.Activate();
            _lineRenderer.SetViewProjectionMatrix(viewProjMtx);

            var entities = _braidGame.GetEntities();
            foreach (var entity in entities)
            {
                if (RenderSettings.RenderEntityBounds)
                    RenderEntityBounds(entity);

                if (RenderSettings.RenderEntityCenters)
                    RenderEntityCenter(entity);
            }
        }

        if (RenderSettings.RenderBorder)
        {
            const float borderWidth = 1f;
            var sw = _braidGame.ScreenWidth.Value;
            var sh = _braidGame.ScreenHeight.Value;
            _lineRenderer.Activate();
            _lineRenderer.SetViewProjectionMatrix(screenMtx);
            _lineRenderer.RenderRectangle(new(sw * .5f, sh * .5f), sw, sh, new(0x200000ff), borderWidth, 0f);
        }

        if (RenderSettings.RenderTimVelocity != TextPosition.None)
        {
            _textRenderer.Activate();
            _textRenderer.SetViewProjectionMatrix(screenMtx);

            var tim = _braidGame.GetTim();
            var timScreenPos = Vector2.Transform(tim.Position, worldToScreenMtx);

            var screenRect = _braidGame.GetScreenRectangle();
            screenRect.Inflate(-10f, -7f); // Padding

            var (alignX, alignY) = RenderSettings.RenderTimVelocity.ToAlignment();
            var alignmentAnchor = RenderSettings.RenderTimVelocity is TextPosition.BelowEntity ? timScreenPos : screenRect.GetAlignmentAnchor(alignX, alignY);

            _textRenderer.RenderText(
                $"velocity\nx={tim.VelocityX:0}\ny={tim.VelocityY:0}",
                alignmentAnchor,
                alignX,
                alignY,
                RenderSettings.FontSize,
                RenderSettings.FontColor,
                lineSpacing: .9f);
        }
    }

    public void RenderPlayerLabelsAndLeaderboard(List<PlayerSummary> players, IReadOnlyList<ChatMessage> chatLog)
    {
        if (_braidGame.InMainMenu)
            return;

        GetMatrices(out var _, out var screenMtx, out var worldToScreenMtx);

        _textRenderer.Activate();
        _textRenderer.SetViewProjectionMatrix(screenMtx);

        // Render visible players
        if (!_braidGame.InPuzzleAssemblyScreen)
        {
            var world = _braidGame.TimWorld.Value;
            var level = _braidGame.TimLevel.Value;
            var visiblePlayers = players.Where(x => x.EntitySnapshot.World == world && x.EntitySnapshot.Level == level).ToList();

            if (visiblePlayers.Count > 0)
            {
                var chatLogByPlayerId = chatLog.Where(x => !x.Stale).GroupBy(x => x.SenderPlayerId).ToDictionary(x => x.Key, x => x.OrderByDescending(x => x.Received).Take(5).ToList());

                foreach (var visiblePlayer in visiblePlayers)
                {
                    var fadedColor = Color4.Multiply(visiblePlayer.Color, new Color4(_braidGame.EntityVertexColorScale)).ToColor();
                    var playerScreenPos = Vector2.Transform(visiblePlayer.EntitySnapshot.Position, worldToScreenMtx);

                    // Render player name below sprite
                    if (!visiblePlayer.IsOwnPlayer)
                        _textRenderer.RenderText(
                            visiblePlayer.Name,
                            playerScreenPos,
                            HAlign.Center,
                            VAlign.Top,
                            RenderSettings.FontSize,
                            fadedColor);

                    // Render speech bubble above player sprite
                    if (chatLogByPlayerId.TryGetValue(visiblePlayer.PlayerId, out var playerChatLog))
                    {
                        const float bubbleLineSpacing = 1.5f;
                        const float bubblePadding = 15f;
                        const float bubbleTextMaxWidth = 200f;
                        var bubbleBgColor = new Color(.0f, .0f, .0f, .5f);
                        var bubbleBottomCenter = playerScreenPos + new Vector2(0f, -118f); // Screen coordinates are "upside down"

                        foreach (var (playerChatEntry, i) in playerChatLog.Select((x, i) => (x, i)))
                        {
                            var bubbleText = _textRenderer.LineBreakToFitMaxWidth(playerChatEntry.Message.Trim(), RenderSettings.FontSize, bubbleTextMaxWidth, out var textSize, bubbleLineSpacing);
                            var bubbleSize = textSize + new Vector2(bubblePadding * 2f);
                            var bubbleRect = new Rect(bubbleSize) { BottomCenter = bubbleBottomCenter };
                            var bubbleTailSize = i == 0 ? new Vector2(10f, 15f) : Vector2.Zero;
                            var bubbleTriangles = Geometry.GetSpeechBubbleTriangleList(bubbleRect, bubbleTailSize, bubblePadding);
                            var bubblePrimitives = new Primitives<TexturedVertex>(_device, PrimitiveType.TriangleList, bubbleTriangles, useVertexBuffer: false);

                            _simpleRenderer.Activate();
                            _simpleRenderer.Render(bubblePrimitives, bubbleBgColor);

                            _textRenderer.Activate();
                            _textRenderer.RenderText(
                                bubbleText,
                                bubbleRect.Center,
                                HAlign.Center,
                                VAlign.Middle,
                                RenderSettings.FontSize,
                                fadedColor,
                                bubbleLineSpacing);

                            bubbleBottomCenter.Y -= bubbleRect.Height;
                        }
                    }
                }
            }
        }

        // Render leaderboard
        foreach (var (player, i) in players.OrderByLeaderboardPosition().Select((x, i) => (x, i)))
        {
            const float margin = 10f;
            const float lineHeight = 1.5f;

            var text = $"{player.PuzzlePieces} pcs ({player.EntitySnapshot.World}-{player.EntitySnapshot.Level}) {player.Name}";

            // Render speedrun timers when in puzzle screen
            if (_braidGame.InPuzzleAssemblyScreen)
                text = $"{text} ({player.FormatSpeedrunTime()})";

            _textRenderer.RenderText(
                text,
                new Vector2(margin, margin + RenderSettings.FontSize * lineHeight * i),
                HAlign.Left,
                VAlign.Top,
                RenderSettings.FontSize,
                player.Color);
        }
    }

    public void RenderChat(IReadOnlyList<ChatMessage> chatLog, bool inputActive, string? chatInput, Color inputColor)
    {
        if (_braidGame.InMainMenu)
            return;

        const int maxPrevMessages = 10;
        var prevMessages = chatLog.Where(x => inputActive || !x.Stale).OrderByDescending(x => x.Received).Take(maxPrevMessages).ToList();
        if (!inputActive && prevMessages.Count == 0)
            return;

        GetMatrices(out var _, out var screenMtx, out var worldToScreenMtx);

        const float paddingX = 10f;
        const float paddingY = 12f;
        const float lineSpacing = 1.5f;
        var chatWindowBottom = _braidGame.ScreenHeight;
        float GetChatLineY(int lineIndex) => chatWindowBottom - paddingY - lineSpacing * lineIndex * RenderSettings.FontSize;

        // Render background
        if (inputActive)
        {
            _simpleRenderer.Activate();
            _simpleRenderer.SetViewProjectionMatrix(screenMtx);

            var lineCount = 1; // Show only chat input line by default
            if (prevMessages.Count > 0)
                lineCount += prevMessages.Count + 1; // Show previous messages plus one additional empty line for spacing

            var bgHeight = TextRenderer.GetTextHeight(lineCount, RenderSettings.FontSize, lineSpacing) + paddingY * 2f;
            var bgColor = new Color(.0f, .0f, .0f, .5f);
            var bgRect = new Rect(_braidGame.ScreenWidth, bgHeight) { Bottom = chatWindowBottom };
            var bgRectPrimitives = new Primitives<TexturedVertex>(_device, PrimitiveType.TriangleList, Geometry.GetRectangleTriangleList(bgRect), useVertexBuffer: false);
            _simpleRenderer.Render(bgRectPrimitives, bgColor);
        }

        _textRenderer.Activate();
        _textRenderer.SetViewProjectionMatrix(screenMtx);

        // Render previous messages
        foreach (var (chatMessage, i) in prevMessages.Select((x, i) => (x, i)))
            _textRenderer.RenderText(
                (!string.IsNullOrWhiteSpace(chatMessage.Sender) ? $"{chatMessage.Sender}: " : "") + chatMessage.Message,
                new Vector2(paddingX, GetChatLineY(i + 2)),
                HAlign.Left,
                VAlign.Bottom,
                RenderSettings.FontSize,
                chatMessage.Color);

        // Render new message input
        if (inputActive)
            _textRenderer.RenderText(
                $"Chat: {chatInput}",
                new Vector2(paddingX, GetChatLineY(0)),
                HAlign.Left,
                VAlign.Bottom,
                RenderSettings.FontSize,
                inputColor);
    }

    private void GetMatrices(out Matrix4x4 viewProjMtx, out Matrix4x4 screenMtx, out Matrix4x4 worldToScreenMtx)
    {
        viewProjMtx = Matrix4x4.CreateOrthographicOffCenter(
           _braidGame.CameraPositionX,
           _braidGame.CameraPositionX + _braidGame.IdealWidth,
           _braidGame.CameraPositionY,
           _braidGame.CameraPositionY + _braidGame.IdealHeight,
           0f,
           1f);

        screenMtx = Matrix4x4.CreateOrthographicOffCenter(
           0f,
           _braidGame.ScreenWidth,
           _braidGame.ScreenHeight,
           0f,
           0f,
           1f);

        worldToScreenMtx = Matrix4x4.Invert(screenMtx, out var inverted) ? viewProjMtx * inverted : throw new Exception($"Failed to invert {nameof(screenMtx)}");

        // Transpose matrices for Direct3D
        viewProjMtx = Matrix4x4.Transpose(viewProjMtx);
        screenMtx = Matrix4x4.Transpose(screenMtx);
    }

    private void RenderEntityBounds(Entity entity)
    {
        var color = GetEntityLineColor(entity);
        if (color is null)
            return;

        if (entity.IsRectangular())
            _lineRenderer.RenderRectangle(entity.Center, entity.Width, entity.Height, color.Value, RenderSettings.LineWidth, entity.Theta);
        else
            _lineRenderer.RenderCircle(entity.Center, entity.GetCircleRadius(), color.Value, RenderSettings.LineWidth);
    }

    private void RenderEntityCenter(Entity entity)
    {
        var color = GetEntityLineColor(entity);
        if (color is null)
            return;

        _lineRenderer.RenderPlusSign(entity.Center, 8f, color.Value, RenderSettings.LineWidth, entity.Theta);
    }

    private Color? GetEntityLineColor(Entity entity)
    {
        Color? color = entity.EntityType.Value switch
        {
            EntityType.Guy => new(1f, 1f, 1f, 1f),
            EntityType.Claw => new(0f, 1f, 0f, 1f),
            EntityType.Bullet or
            EntityType.Cannon or
            EntityType.Chandelier or
            EntityType.ChandelierHook or
            EntityType.Cloud or
            EntityType.Door or
            EntityType.Flagpole or
            EntityType.Floor or
            EntityType.Gate or
            EntityType.GunBoss or
            EntityType.Key or
            EntityType.Mimic or
            EntityType.Monstar or
            EntityType.Platform or
            EntityType.Prince or
            EntityType.Princess or
            EntityType.PuzzleFrame or
            EntityType.PuzzlePiece or
            EntityType.Ring or
            EntityType.SpecialItem => entity.IsMonster() ? new(1f, 0f, 0f, 1f) : new(0f, 0f, 1f, 1f),
            EntityType.PiecedImage => entity.IsClimbable ? new(1f, .5f, 0f, 1f) : null,
            EntityType.Tiler => new(1f, 1f, 0f, 1f),
            _ => null,
        };

        if (color != null && RenderSettings.IsLineColorActive())
            color = RenderSettings.LineColor;

        return color;
    }
}
