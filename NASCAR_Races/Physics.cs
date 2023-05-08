﻿using System;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace NASCAR_Races
{
    public class Physics
    {
        public float X { get; private set; } = 0;
        public float Y { get; private set; } = 0;
        public float Length { get; private set; } = 15;
        public float Width { get; private set; } = 10;
        public float Speed { get; private set; } = 0;
        public float HeadingAngle { get; set; } = 0;

        const float accelerationOfGravity = 9.81f;
        const float trackAngle = 0.175f;
        const float airDensity = 1.225f;
        const float frontSurface = 2.5f;
        const float carAirDynamic = 0.35f;
        //change to private for logs only
        public float _currentAcceleration;
        private float _mass;
        private float _frictionofweels;
        private System.DateTime _lastExecutionTime;

        protected Point _leftCircle { get; set; }
        protected Point _rightCircle { get; set; }
        protected int _circleRadius { get; set; }

        private float _turnRadius;
        private float _UseOftires = 0.5f;

        protected float FuelMass;
        protected float FuelCapacity;
        protected float FuelBurningRatio = 0.5f;

        protected float MaxHorsePower { get; private set; }
        protected float CurrentHorsePower;
        protected float BrakesForce = 50000;

        protected List<Car> _neighbouringCars;

        protected bool _recalculateHeadingAngle = false;

        private double currentTurnAngle = -Math.PI / 2;

        private Worldinformation _worldInf;

        //LOGS
        public bool isbraking = false;

        public Physics() { }
        public Physics(float x, float y, float mass, float fuelCapacity, float frictionofweels, float maxHorsePower, Worldinformation worldInfo)
        {
            X = x;
            Y = y;
            _mass = mass;
            _turnRadius = worldInfo.TurnRadius;
            FuelMass = fuelCapacity;
            FuelCapacity = fuelCapacity;
            _frictionofweels = frictionofweels;
            MaxHorsePower = maxHorsePower;
            CurrentHorsePower = MaxHorsePower;
            _lastExecutionTime = DateTime.Now;

            _worldInf = worldInfo;
        }
        // Run in the loop
        public void RunPhysic()
        {
            DateTime currentTime = DateTime.Now;
            TimeSpan timeSinceLastExecution = currentTime - _lastExecutionTime;
            //float FF = FrictionForce(); // siła która przeciwdziała sile dośrodkowej
            float timeTemp = (float)timeSinceLastExecution.TotalSeconds;
            float wheelFriction = 60;
            float airR = AirResistance();
            _currentAcceleration = (AccelerationForce() - airR - wheelFriction) / _mass;
            Speed += (float)(_currentAcceleration * timeTemp);

            //if (Acceleration() < AirR + wheelFriction) _currentAcceleration = 0;
            //FuelMass -= CurrentHorsePower * FuelBurningRatio; // * time
            var partOfCircuit = WhatPartOfCircuitIsCarOn();
            if (partOfCircuit == Worldinformation.CIRCUIT_PARTS.RIGHT_TURN)
            {
                MoveCarOnCircle((float)timeSinceLastExecution.TotalSeconds, true, _rightCircle);
            }
            else if (partOfCircuit == Worldinformation.CIRCUIT_PARTS.LEFT_TURN)
            {
                MoveCarOnCircle((float)timeSinceLastExecution.TotalSeconds, false, _leftCircle);
            }
            else
            {
                MoveCarOnStraight((float)timeSinceLastExecution.TotalSeconds, partOfCircuit);
                _recalculateHeadingAngle = false;
            }

            _lastExecutionTime = currentTime;
        }
        private void Braking(float timeTemp)
        {
            isbraking = true;
            CurrentHorsePower = 0;
            Speed -= timeTemp * BrakingForce() / _mass;
        }
        private void notBraking()
        {
            isbraking = false;
            CurrentHorsePower = MaxHorsePower;
        }
        private void MoveCarOnCircle(float timeElapsed, bool rightCircleControll, Point circle)
        {
            if (!_recalculateHeadingAngle)
            {
                currentTurnAngle = CalculateEnteringAngle(rightCircleControll);
                _recalculateHeadingAngle = true;
            }
            //distanceToEndOfTheTrack = positionofcar - positionofBorder
            //float r = DistanceFromPointToPoint(X, Y, circle.X, circle.Y);
            float r = _circleRadius;

            // Wyznaczamy nowy kąt, uwzględniając czas i prędkość
            float a = circle.X;
            float b = circle.Y;
            /*if (IscentrifugalForce(_circleRadius) != 0)
            {
                Braking(timeElapsed);
            }
            else
            {
                notBraking();
            }*/
            currentTurnAngle += Speed * timeElapsed / r;
            HeadingAngle = -(float)((currentTurnAngle + Math.PI / 2) * (180.0 / Math.PI));
            // Wyznaczamy nowe współrzędne X i Y samochodu
            X = a + r * (float)Math.Cos(-currentTurnAngle);
            Y = b + r * (float)Math.Sin(-currentTurnAngle);
        }
        private void MoveCarOnStraight(float timeElapsed, Worldinformation.CIRCUIT_PARTS partOfCircuit)
        {
            if (partOfCircuit == Worldinformation.CIRCUIT_PARTS.TOP)
            {
                //TOP
                // if car in front is slower then go to the left
                (float, float) temp = DistanceToAndSpeedOfOpponentInFront();
                if (temp.Item1 < float.MaxValue && temp.Item2 < Speed)
                {
                    if (DistanceToOpponentOnLeft() > 1)
                    {
                        Y += 0.5f;
                    }
                }
                else if (DistanceToOpponentOnRight() > 1 && _worldInf.DistanceToEdgeOfTrack(this) > 1)
                {
                    Y -= 0.5f;
                }
                X -= Speed * timeElapsed;
                HeadingAngle = 0;
                
            }
            else if (partOfCircuit == Worldinformation.CIRCUIT_PARTS.BOTTOM)
            {
                //BOTTOM
                (float, float) temp = DistanceToAndSpeedOfOpponentInFront();
                if (temp.Item1 < float.MaxValue && temp.Item2 < Speed)
                {
                    if (DistanceToOpponentOnLeft() > 1)
                    {
                        Y -= 0.5f;
                    }
                }
                else if (DistanceToOpponentOnRight() > 1 && _worldInf.DistanceToEdgeOfTrack(this) > 1)
                {
                    Y += 0.5f;
                }
                X += Speed * timeElapsed;
                HeadingAngle = 180;
            }

            /*if (IscentrifugalForce(_circleRadius) != 0 && ((_leftPerfectCircle.Y > Y && X < _leftPerfectCircle.X + _circleRadius / 2) || (_rightPerfectCircle.Y < Y && X > _rightPerfectCircle.X - _circleRadius / 2)))
            {
                Braking(timeElapsed);
            }
            else
            {
                notBraking();
            }*/
        }
        public float DistanceFromPointToPoint(float x1, float y1, float x2, float y2)
        {
            return (float)Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));
        }
        //sila odsrodkowa
        public float CentrifugalForce(float radius)
        {
            float CenForce = _mass * (float)Math.Pow(Speed, 2) / radius;
            //Console.WriteLine("Cen Force: " + CenForce);
            return CenForce;
        }
        //siła tarcia
        public float FrictionForce()
        {
            return (float)(_mass * _frictionofweels * accelerationOfGravity);
            //To DO include trackAngle  * this.trackAngle
        }
        //Opór powietrza
        public float AirResistance()
        {
            return (float)(0.5 * airDensity * (float)Math.Pow(Speed, 2) * carAirDynamic * frontSurface);
        }
        // ile samochód się oddala/przybliża do środka
        private float AccelerationForce()
        {
            float efficency = 1 - (Speed / 100);
            return CurrentHorsePower * efficency;
        }
        private float BrakingForce()
        {
            return BrakesForce * _UseOftires;
        }
        public float IscentrifugalForce(float radius)
        {
            //* (float)Math.Cos(trackAngle)
            float CF = CentrifugalForce(radius);
            float Fofx = CF * (float)Math.Cos(trackAngle);
            float Fofy = CF * (float)Math.Sin(trackAngle);

            float GravityForce = _mass * accelerationOfGravity;
            float GravityForce_X = GravityForce * (float)Math.Sin(trackAngle);
            float GravityForce_Y = GravityForce * (float)Math.Cos(trackAngle);

            float frictionAll = (GravityForce_Y + Fofy) * _frictionofweels;
            float XFroce = Fofx - GravityForce_X;

            //nie ma pośligu
            if (frictionAll >= Math.Abs(XFroce))
            {
                return 0;
            }
            //jest poślizg
            else
            {
                if (XFroce > 0)
                    return XFroce - frictionAll;
                else
                    return frictionAll - XFroce;
            }
        }
        protected void FindCircle(int y, bool righCircleControl)
        {
            _circleRadius = Math.Abs(y - _worldInf.CanvasCenterY);
            int x;
            if (righCircleControl)
            {
                float distanceToEdgeOfTrackInTheMiddleOfTurn = _worldInf.DistanceToEdgeOfTrack(this, false);
                if (DistanceToOpponentOnLeft() < distanceToEdgeOfTrackInTheMiddleOfTurn)
                {
                    distanceToEdgeOfTrackInTheMiddleOfTurn -= Width;
                }
                x = _worldInf.x2 + _worldInf.TurnRadius - _circleRadius + _worldInf.PenCircuitSize / 2 - (int)distanceToEdgeOfTrackInTheMiddleOfTurn;
                _rightCircle = new Point(x, _worldInf.CanvasCenterY);
            }
            else
            {
                float distanceToEdgeOfTrackInTheMiddleOfTurn = _worldInf.DistanceToEdgeOfTrack(this, false);
                if (DistanceToOpponentOnLeft() < distanceToEdgeOfTrackInTheMiddleOfTurn)
                {
                    distanceToEdgeOfTrackInTheMiddleOfTurn -= Width;
                }
                x = _worldInf.x1 - _worldInf.TurnRadius + _circleRadius - _worldInf.PenCircuitSize / 2 + (int)distanceToEdgeOfTrackInTheMiddleOfTurn;
                _leftCircle = new Point(x, _worldInf.CanvasCenterY);
            }
        }
        //return:
        //distance to opponent on right if there is any
        //distance to the edge of circuit
        protected float DistanceToOpponentOnRight()
        {
            float distance = _worldInf.DistanceToEdgeOfTrack(this);
            foreach (Car car in _neighbouringCars)
            {
                if (Math.Abs(X - car.X) > Length / 2 + car.Length / 2) continue;
                float temp;
                switch (_worldInf.WhatPartOfCircuitIsCarOn(this))
                {
                    case Worldinformation.CIRCUIT_PARTS.LEFT_TURN:

                        break;
                    case Worldinformation.CIRCUIT_PARTS.RIGHT_TURN:

                        break;
                    case Worldinformation.CIRCUIT_PARTS.TOP:
                        //Car will enter "left" turn
                        if (car.Y < Y)
                        {
                            //opponent is on the right side of this car
                            temp = (Y - Width / 2) - (car.Y + car.Width / 2);
                            if (temp < distance) distance = temp;
                        }
                        break;
                    case Worldinformation.CIRCUIT_PARTS.BOTTOM:
                        //Car will enter "right" turn
                        if (car.Y > Y)
                        {
                            //opponent is on the right side of this car
                            temp = (car.Y - car.Width / 2) - (Y + Width / 2);
                            if (temp < distance) distance = temp;
                        }
                        break;
                    case Worldinformation.CIRCUIT_PARTS.PIT:

                        break;
                }
            }
            return distance;
        }
        //returns:
        //distance to first opponent on the left, regardless of his X coordinates
        //distance to edge of track if there are no opponents on left side
        protected float DistanceToOpponentOnLeft()
        {
            float distance = _worldInf.DistanceToEdgeOfTrack(this, false);
            foreach (Car car in _neighbouringCars)
            {
                if (Math.Abs(X - car.X) > Length / 2 + car.Length / 2) continue;
                float temp;
                switch (_worldInf.WhatPartOfCircuitIsCarOn(this))
                {
                    case Worldinformation.CIRCUIT_PARTS.LEFT_TURN:

                        break;
                    case Worldinformation.CIRCUIT_PARTS.RIGHT_TURN:

                        break;
                    case Worldinformation.CIRCUIT_PARTS.TOP:
                        //Car will enter "left" turn
                        if (car.Y > Y)
                        {
                            //opponent is on the left side of this car
                            temp = car.Y - car.Width / 2 - Y + Width / 2;
                            if (temp < distance) distance = temp;
                        }
                        break;
                    case Worldinformation.CIRCUIT_PARTS.BOTTOM:
                        //Car will enter "right" turn
                        if (car.Y < Y)
                        {
                            //opponent is on the left side of this car
                            temp = Y - Width / 2 - car.Y + car.Width / 2;
                            if (temp < distance) distance = temp;
                        }
                        break;
                    case Worldinformation.CIRCUIT_PARTS.PIT:

                        break;
                }
            }
            return distance;
        }
        protected (float, float) DistanceToAndSpeedOfOpponentInFront()
        {
            float distance = float.MaxValue;
            Car tempCar = null;
            foreach (Car car in _neighbouringCars)
            {
                int temp;
                switch (WhatPartOfCircuitIsCarOn())
                {
                    case Worldinformation.CIRCUIT_PARTS.TOP:
                        if (car.X > X) continue;
                        if (Math.Abs(car.Y - Y) > Width / 2 + car.Width / 2) continue;
                        temp = (int)((X - Length / 2) - (car.X + car.Length / 2));
                        if (temp < distance)
                        {
                            distance = temp;
                            tempCar = car;
                        }
                        break;
                    case Worldinformation.CIRCUIT_PARTS.BOTTOM:
                        if (car.X < X) continue;
                        if (Math.Abs(car.Y - Y) > Width / 2 + car.Width / 2) continue;
                        temp = (int)((car.X - car.Length / 2) - (X + Length / 2));
                        if (temp < distance)
                        {
                            distance = temp;
                            tempCar = car;
                        }
                        break;
                    default:
                        break;
                }
            }
            return (distance, (tempCar != null) ? tempCar.Speed : 0);
        }
        private float CalculateEnteringAngle(bool rightTurnControl)
        {
            if (rightTurnControl)
            {
                if (Y < _worldInf.CanvasCenterY)
                {
                    //TOP RIGHT
                    double alpha = Math.Asin(Math.Abs(Y - _rightCircle.Y) / _circleRadius);
                    return (float)alpha;
                }
                else
                {
                    //BOTTOM RIGHT
                    double alpha = Math.Asin(Math.Abs(X - _rightCircle.X) / _circleRadius);
                    return (float)(alpha - Math.PI / 2);
                }
            }
            else
            {
                if (Y < _worldInf.CanvasCenterY)
                {
                    //TOP LEFT
                    double alpha = Math.Asin(Math.Abs(X - _leftCircle.X) / _circleRadius);
                    return (float)(alpha + Math.PI / 2);
                }
                else
                {
                    //BOTTOM LEFT
                    double alpha = Math.Asin(Math.Abs(Y - _leftCircle.Y) / _circleRadius);
                    return (float)(alpha + Math.PI);
                }
            }
        }
        public Worldinformation.CIRCUIT_PARTS WhatPartOfCircuitIsCarOn()
        {
            if (X < _leftCircle.X) return Worldinformation.CIRCUIT_PARTS.LEFT_TURN;
            if (X > _rightCircle.X) return Worldinformation.CIRCUIT_PARTS.RIGHT_TURN;
            if (Y < _worldInf.CanvasCenterY) return Worldinformation.CIRCUIT_PARTS.TOP;
            return Worldinformation.CIRCUIT_PARTS.BOTTOM;
        }

    }
}