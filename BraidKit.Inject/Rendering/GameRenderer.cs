﻿using BraidKit.Core;
using BraidKit.Core.Game;
using System.Numerics;
using Vortice.Direct3D9;
using Vortice.Mathematics;

namespace BraidKit.Inject.Rendering;

internal class GameRenderer(BraidGame _braidGame, IDirect3DDevice9 _device) : IDisposable
{
    private readonly LineRenderer _lineRenderer = new(_device);
    private readonly TextRenderer _textRenderer = new(_device);
    public RenderSettings RenderSettings { get; set; } = new();

    public void Dispose()
    {
        _lineRenderer.Dispose();
        _textRenderer.Dispose();
    }

    public void Render()
    {
        if (!RenderSettings.IsRenderingActive() || _braidGame.InMainMenu || _braidGame.InPuzzleAssemblyScreen)
            return;

        var viewProjMtx = Matrix4x4.Transpose(Matrix4x4.CreateOrthographicOffCenter(
            _braidGame.CameraPositionX,
            _braidGame.CameraPositionX + _braidGame.IdealWidth,
            _braidGame.CameraPositionY,
            _braidGame.CameraPositionY + _braidGame.IdealHeight,
            0f,
            1f));

        var screenMtx = Matrix4x4.Transpose(Matrix4x4.CreateOrthographicOffCenter(
            0f,
            _braidGame.ScreenWidth,
            _braidGame.ScreenHeight,
            0f,
            0f,
            1f));

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

        if (RenderSettings.RenderTimVelocity != TextPosition.None)
        {
            var useWorldCoords = RenderSettings.RenderTimVelocity == TextPosition.BelowEntity;

            _textRenderer.Activate();
            _textRenderer.SetViewProjectionMatrix(useWorldCoords ? viewProjMtx : screenMtx);

            var tim = _braidGame.GetTim();

            const float marginX = 10f;
            var (alignX, textX) = RenderSettings.RenderTimVelocity switch
            {
                TextPosition.TopLeft or
                TextPosition.MiddleLeft or
                TextPosition.BottomLeft => (HAlign.Left, marginX),
                TextPosition.TopCenter or
                TextPosition.MiddleCenter or
                TextPosition.BottomCenter => (HAlign.Center, _braidGame.ScreenWidth * .5f),
                TextPosition.TopRight or
                TextPosition.MiddleRight or
                TextPosition.BottomRight => (HAlign.Right, _braidGame.ScreenWidth - marginX),
                TextPosition.BelowEntity => (HAlign.Center, tim.PositionX),
                _ => throw new ArgumentOutOfRangeException(nameof(RenderSettings.RenderTimVelocity), RenderSettings.RenderTimVelocity, null),
            };

            const float marginY = 7f;
            var (alignY, textY) = RenderSettings.RenderTimVelocity switch
            {
                TextPosition.TopLeft or
                TextPosition.TopCenter or
                TextPosition.TopRight => (VAlign.Top, marginY),
                TextPosition.MiddleLeft or
                TextPosition.MiddleCenter or
                TextPosition.MiddleRight => (VAlign.Middle, _braidGame.ScreenHeight * .5f),
                TextPosition.BottomLeft or
                TextPosition.BottomCenter or
                TextPosition.BottomRight => (VAlign.Bottom, _braidGame.ScreenHeight - marginY),
                TextPosition.BelowEntity => (VAlign.Top, tim.PositionY),
                _ => throw new ArgumentOutOfRangeException(nameof(RenderSettings.RenderTimVelocity), RenderSettings.RenderTimVelocity, null),
            };

            _textRenderer.RenderText($"velocity\nx={tim.VelocityX:0}\ny={tim.VelocityY:0}",
                textX,
                textY,
                alignX,
                alignY,
                RenderSettings.FontSize,
                new(RenderSettings.FontColor),
                useWorldCoords);
        }
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

    private Color4? GetEntityLineColor(Entity entity)
    {
        Color4? color = entity.EntityType.Value switch
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
            color = new(RenderSettings.LineColor);

        return color;
    }
}
