using BraidKit.Core;
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

        if (RenderSettings.RenderTimVelocity)
        {
            _textRenderer.Activate();
            _textRenderer.SetViewProjectionMatrix(viewProjMtx);

            var tim = _braidGame.GetTim();
            _textRenderer.RenderText($"velocity\nx={tim.VelocityX:0}\ny={tim.VelocityY:0}",
                tim.PositionX,
                tim.PositionY,
                true,
                false,
                RenderSettings.FontSize,
                new(RenderSettings.FontColor));
        }
    }

    private void RenderEntityBounds(Entity entity)
    {
        var color = GetEntityBoundsColor(entity);
        if (color is null)
            return;

        if (entity.IsRectangular())
            _lineRenderer.RenderRectangle(entity.Center, entity.Width, entity.Height, color.Value, RenderSettings.LineWidth, entity.Theta);
        else
            _lineRenderer.RenderCircle(entity.Center, entity.GetCircleRadius(), color.Value, RenderSettings.LineWidth);
    }

    private void RenderEntityCenter(Entity entity)
    {
        var color = GetEntityBoundsColor(entity);
        if (color is null)
            return;

        _lineRenderer.RenderPlusSign(entity.Center, 8f, color.Value, RenderSettings.LineWidth, entity.Theta);
    }

    private static Color4? GetEntityBoundsColor(Entity entity) => entity.EntityType.Value switch
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
}
