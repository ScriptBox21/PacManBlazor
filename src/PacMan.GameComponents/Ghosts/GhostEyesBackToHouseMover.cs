﻿using System;
using System.Numerics;
using System.Threading.Tasks;
using MediatR;
using PacMan.GameComponents.Canvas;
using PacMan.GameComponents.Events;

namespace PacMan.GameComponents.Ghosts
{
    /// Moves the ghost back to the house.
    public class GhostEyesBackToHouseMover : GhostMover
    {
        readonly IMediator _mediator;
        readonly Vector2 _ghostPosInHouse;
        Func<CanvasTimingInformation, ValueTask<MovementResult>> _currentAction;

        public GhostEyesBackToHouseMover(Ghost ghost, IMaze maze, IMediator mediator)
            : base(ghost, GhostMovementMode.GoingToHouse, maze, () => new ValueTask<CellIndex>(Maze.TileHouseEntrance.ToCellIndex()))
        {
            _mediator = mediator;
            _ghostPosInHouse = Maze.PixelCenterOfHouse + new Vector2(ghost.OffsetInHouse * 16, 0);

            _currentAction = navigateEyesBackToJustOutsideHouse;
        }

        async ValueTask<MovementResult> navigateEyesBackToJustOutsideHouse(CanvasTimingInformation context)
        {
            await base.Update(context);

            if (isNearHouseEntrance())
            {
                await _mediator.Publish(new GhostInsideHouseEvent());
                Ghost.Position = Maze.PixelHouseEntrancePoint;
                _currentAction = navigateToCenterOfHouse;
            }

            return MovementResult.NotFinished;
        }

        ValueTask<MovementResult> navigateToCenterOfHouse(CanvasTimingInformation context)
        {
            var diff = Maze.PixelCenterOfHouse - Maze.PixelHouseEntrancePoint;

            if (diff != Vector2.Zero)
            {
                diff = diff.Normalize();
                Ghost.Position = Ghost.Position + diff;
            }

            if (Ghost.Position.Round() == Maze.PixelCenterOfHouse)
            {
                _currentAction = navigateToGhostIndexInHouse;
            }

            return new ValueTask<MovementResult>(MovementResult.NotFinished);
        }

        ValueTask<MovementResult> navigateToGhostIndexInHouse(CanvasTimingInformation context)
        {
            var diff = _ghostPosInHouse - Maze.PixelCenterOfHouse;

            if (diff != Vector2.Zero)
            {
                diff = diff.Normalize();
                Ghost.Position = Ghost.Position + diff;
            }

            if (Ghost.Position.Round() == _ghostPosInHouse)
            {
                Ghost.Direction = new DirectionInfo(Directions.Down, Directions.Down);
                Ghost.SetMovementMode(GhostMovementMode.InHouse);
                return new ValueTask<MovementResult>(MovementResult.Finished);
            }

            return new ValueTask<MovementResult>(MovementResult.NotFinished);
        }

        public override async ValueTask<MovementResult> Update(CanvasTimingInformation context) => await _currentAction(context);

        bool isNearHouseEntrance() => Vector2s.AreNear(Ghost.Position, Maze.PixelHouseEntrancePoint, .75);
    }
}