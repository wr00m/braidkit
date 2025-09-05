using BraidKit.Core.Game;
using System.Numerics;
using Vortice.Direct3D9;
using Vortice.Mathematics;

namespace BraidKit.Inject.Rendering;

internal class GameRenderer(BraidGame _braidGame, IDirect3DDevice9 _device) : IDisposable
{
    private readonly LineRenderer _lineRenderer = new(_device);

    public float LineWidth { get => _lineRenderer.LineWidth; set => _lineRenderer.LineWidth = value; }

    public void Dispose()
    {
        _lineRenderer?.Dispose();
    }

    public void RenderCollisionGeometries()
    {
        var viewProjMtx = Matrix4x4.Transpose(Matrix4x4.CreateOrthographicOffCenter(
            _braidGame.CameraPositionX,
            _braidGame.CameraPositionX + _braidGame.IdealWidth,
            _braidGame.CameraPositionY,
            _braidGame.CameraPositionY + _braidGame.IdealHeight,
            0f,
            1f));

        _lineRenderer.Activate();
        _lineRenderer.SetViewProjectionMatrix(viewProjMtx);

        var entities = _braidGame.GetEntities();
        foreach (var entity in entities)
            RenderCollisionGeometry(entity);
    }

    private void RenderCollisionGeometry(Entity entity)
    {
        var color = entity.EntityType.Value switch
        {
            EntityType.Guy => new Color4(1f, 1f, 1f, 1f),
            EntityType.Claw => new Color4(0f, 1f, 0f, 1f),
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
            EntityType.SpecialItem => entity.IsMonster() ? new Color4(1f, 0f, 0f, 1f) : new Color4(0f, 0f, 1f, 1f),
            _ => (Color4?)null,
        };

        if (color is null)
            return;

        if (entity.IsRectangular())
            _lineRenderer.RenderRectangle(entity.Center, entity.Width, entity.Height, color.Value, entity.Theta);
        else
            _lineRenderer.RenderCircle(entity.Center, entity.GetCircleRadius(), color.Value);
    }
}
