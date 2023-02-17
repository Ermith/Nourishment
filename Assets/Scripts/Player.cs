using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using System;
using UnityEngine.SceneManagement;
using static RootTile;

public class Player : MonoBehaviour
{
    [NonSerialized]
    public int X = World.MapWidth / 2;
    [NonSerialized]
    public int Y = -1;
    public Camera Camera;

    private Tween _playerTween;
    private Tween _cameraTween;

    public static float Lowest = 0;

    // Start is called before the first frame update
    void Start()
    {
        Lowest = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            SceneManager.LoadScene("MainMenuScene");

        if (Input.GetKeyDown(KeyCode.Space))
            Util.GetWorld().SimulationStep(true);

        if (Input.GetKeyDown(KeyCode.X))
            Util.GetFlower().HideVictory();

        if (Util.GetWorld().CheatsEnabled)
        {
            if (Input.GetKeyDown(KeyCode.F))
                Util.GetEntityFactory().PlaceEntity(Util.GetWorld().gameObject, EntityType.Slug, X, Y);

            if (Input.GetKeyDown(KeyCode.G))
                Util.GetEntityFactory().PlaceEntity(Util.GetWorld().gameObject, EntityType.Snail, X, Y);

            if (Input.GetKeyDown(KeyCode.B))
                Util.GetEntityFactory().PlaceEntity(Util.GetWorld().gameObject, EntityType.Bee, X, Y);

            if (Input.GetKeyDown(KeyCode.N))
                Util.GetEntityFactory().PlaceEntity(Util.GetWorld().gameObject, EntityType.AmberBee, X, Y);

            if(Input.GetKeyDown(KeyCode.R))
                Util.GetEntityFactory().PlaceEntity(Util.GetWorld().gameObject, EntityType.SmallRock, X, Y);

            if (Input.GetKeyDown(KeyCode.V))
                Util.GetWorld().GetSquare(X, Y).Water.Amount = 1.0f;
        }

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
                Util.GetWorld().SimulationStep(false);
        }

        _playerTween?.Kill();
        _cameraTween?.Kill();

        _playerTween = gameObject.transform.DOMove(new Vector3(X - World.MapWidth / 2, Y, 0), 0.2f);

        var topmostCameraPos = 5 - Camera.orthographicSize;
        var cameraY = Mathf.Min(Y, topmostCameraPos);
        if (Camera.transform.position.y > topmostCameraPos)
            Camera.transform.position = new Vector2(0f, topmostCameraPos);
        else
            _cameraTween = Camera.transform.DOMoveY(cameraY, 0.4f);
    }

    public bool TryMove(Direction direction, bool retreat)
    {
        var square = Util.GetWorld().GetSquare(X + direction.X(), Y + direction.Y());
        if (square == null)
            return false;
        if (!square.CanSpread(this, direction))
            return false;
        World world = Util.GetWorld();
        int oldX = X;
        int oldY = Y;
        int newX = X + direction.X();
        int newY = Y + direction.Y();

        if (newY > -1)
            return false;

        RootTile oldTile = world.GetTile(X, Y) as RootTile;
        if (oldTile == null)
        {
            Util.GetFlower().Nourishment = 0.0f; // kill
            return false;
        }

        Tile newTile = world.GetTile(newX, newY);

        if (!newTile.Diggable)
            return false;

        if (Util.GetFlower().Nourishment < newTile.Hardness)
            return false;

        if (retreat && newTile is not RootTile)
            return false;

        if (retreat && oldTile.Status == RootStatus.Initial)
            return false;

        X = newX;
        Y = newY;
        
        if(retreat)
            world.ReplaceTile(oldX, oldY, TileType.Air);
        
        square.OnSpread(this, direction);
        
        if (newTile is RootTile newExistingRootTile)
        {
            var oldConn = !retreat && (oldTile.Status == RootStatus.Connected || oldTile.Status == RootStatus.Initial);
            var newConn = newExistingRootTile.Status == RootStatus.Connected || newExistingRootTile.Status == RootStatus.Initial;
            if (oldConn != newConn || newExistingRootTile.Status == RootStatus.Spawned)
            {
                var newStatus = (oldConn || newConn) ? RootStatus.Connected : RootStatus.Disconnected;
                if (newExistingRootTile.Status != newStatus && newExistingRootTile.Status != RootStatus.Initial)
                    newExistingRootTile.BfsApply(tile =>
                        {
                            tile.Status = newStatus;
                        },
                        tile => tile.Status == RootStatus.Disconnected || tile.Status == RootStatus.Spawned);
                else if (oldTile.Status != newStatus && oldTile.Status != RootStatus.Initial)
                    oldTile.BfsApply(tile =>
                        {
                            tile.Status = newStatus;
                        },
                        tile => tile.Status == RootStatus.Disconnected || tile.Status == RootStatus.Spawned);
            }
        }
        else
        {
            var newRootTile = (RootTile)world.ReplaceTile(X, Y, TileType.Root);
            newRootTile.Status = oldTile.Status switch
            {
                RootStatus.Connected => RootStatus.Connected,
                RootStatus.Initial => RootStatus.Connected,
                RootStatus.Disconnected => RootStatus.Disconnected,
                _ => throw new NotImplementedException(),
            };
            newRootTile.GrowAnim(direction);
        }

        if(!retreat)
            oldTile.ConnectWithNeigh(direction);

        Util.GetFlower().Nourishment -= newTile.Hardness;
        if (newTile is NutritionGroundTile nutritionGroundTile)
            Util.GetFlower().Nourishment += nutritionGroundTile.Nutrition;
        
        Util.GetAudioManager().Play(newTile.Audio);
        Lowest = Mathf.Min(Lowest, Y);

        return true;
    }
}
