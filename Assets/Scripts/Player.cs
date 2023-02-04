using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

public class Player : MonoBehaviour
{
    public int X = World.MAP_WIDTH / 2;
    public int Y = -1;
    public Camera Camera;

    private Tween playerTween;
    private Tween cameraTween;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            Util.GetWorld().SimulationStep();

        if (Input.GetKeyDown(KeyCode.F))
            EntityFactory.PlaceEntity(Util.GetWorld().gameObject, EntityType.Slug, X, Y);

        Direction? movementDir = null;
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
            movementDir = Direction.Up;
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            movementDir = Direction.Down;
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
            movementDir = Direction.Left;
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
            movementDir = Direction.Right;

        bool retreat = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (movementDir != null)
        {
            bool success = TryMove(movementDir.Value, retreat);
            if(success)
                Util.GetWorld().SimulationStep();
        }

        playerTween?.Kill();
        cameraTween?.Kill();

        playerTween = gameObject.transform.DOMove(new Vector3(X - World.MAP_WIDTH / 2, Y, 0), 0.2f);
        cameraTween = Camera.transform.DOMoveY(Y, 0.4f);
    }

    bool TryMove(Direction direction, bool retreat)
    {
        var square = Util.GetWorld().GetSquare(X + direction.X(), Y + direction.Y());
        if (square == null)
            return false;
        if (!square.CanSpread(this, direction))
            return false;
        World world = Util.GetWorld();
        int newX = X + direction.X();
        int newY = Y + direction.Y();

        if (newY > -1)
            return false;

        Tile oldTile = world.GetTile(X, Y);
        Tile newTile = world.GetTile(newX, newY);

        if (!newTile.Diggable)
            return false;

        if (Util.GetFlower().Nourishment < newTile.Hardness)
            return false;

        if (retreat && newTile is not RootTile)
            return false;

        if (oldTile is RootTile oldRoot && retreat)
        {
            if (oldRoot.Protected)
                return false;

            world.ReplaceTile(X, Y, TileType.Air);
            Util.GetFlower().Nourishment++;
        }

        square.OnSpread(this, direction);
        X = newX;
        Y = newY;
        if(newTile is not RootTile)
            world.ReplaceTile(X, Y, TileType.Root);
        if (oldTile is RootTile rootTile)
            rootTile.ConnectWithNeigh(direction);

        Util.GetFlower().Nourishment -= newTile.Hardness;

        return true;
    }
}
