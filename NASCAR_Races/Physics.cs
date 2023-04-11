﻿using System;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace NASCAR_Races
{
    public class Physics
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Speed { get; private set; } = 0;
        public float HeadingAngle { get; set; } = 0;

        const float accelerationOfGravity = 9.81f;
        const float trackAngle = 10;
        const float airDensity = 1.225f;
        const float frontSurface = 2.5f;
        const float carAirDynamic = 0.35f;

        private float _currentAcceleration;
        private float _mass;
        private float _frictionofweels;
        private System.DateTime _lastExecutionTime;

        private Point _leftCircle;
        private Point _rightCircle;

        private Point _leftPerfectCircle;
        private Point _rightPerfectCircle;
        private int _perfectCircleRadius;

        private float _turnRadius;

        protected float GasPedalDepression;

        protected float FuelMass;
        protected float FuelCapacity;
        protected float FuelBurningRatio = 0.5f;

        protected float MaxHorsePower;
        protected float CurrentHorsePower = 4000; // Force should be in the Future

        private double currentTurnAngle = -Math.PI / 2;

        private Worldinformation _worldInf;
        

        public Physics() { }
        public Physics(float x, float y, float mass, float fuelCapacity, float frictionofweels, Worldinformation worldInfo)
        {
            X= x;
            Y= y;
            _mass = mass;
            _leftCircle = worldInfo.LeftCircle;
            _rightCircle = worldInfo.RightCircle;
            _turnRadius = worldInfo.TurnRadius;
            FuelMass = fuelCapacity;
            FuelCapacity = fuelCapacity;
            _frictionofweels = frictionofweels;
            _lastExecutionTime = DateTime.Now;

            _worldInf = worldInfo;

            List<Point>temp = worldInfo.PerfectTurnCirclePoints(false);
            List<double> temp2 = FindCircle(temp);
            _leftPerfectCircle = new Point((int)temp2[0], (int)temp2[1]);
            _perfectCircleRadius = (int)temp2[2];
            temp = worldInfo.PerfectTurnCirclePoints();
            temp2 = FindCircle(temp);
            _rightPerfectCircle=new Point((int)temp2[0], (int)temp2[1]);
        }
        // Run in the loop
        public void RunPhysic()
        {
            DateTime currentTime = DateTime.Now;
            TimeSpan timeSinceLastExecution = currentTime - _lastExecutionTime;

            //float AirR = AirResistance();
            //float FF = FrictionForce(); // siła która przeciwdziała sile dośrodkowej
            //float wheelFriction = 60;
            //_currentAcceleration = Acceleration() - AirR - wheelFriction;
            //if (Acceleration() < AirR + wheelFriction) _currentAcceleration = 0;
            //Speed += _currentAcceleration / _mass; // * time
            Speed = 140f;
            //FuelMass -= CurrentHorsePower * FuelBurningRatio; // * time
            
            if (X > _rightPerfectCircle.X)
            {
                MoveCarOnCircle((float)timeSinceLastExecution.TotalSeconds, true, _rightPerfectCircle);
            }
            else if(X < _leftPerfectCircle.X)
            {
                MoveCarOnCircle((float)timeSinceLastExecution.TotalSeconds, true, _leftPerfectCircle);
            }
            //dodac else if dla lewego okregu
            else
            {
                if (Y <= _worldInf.CanvasHeight / 2)
                {
                    X -= Speed * (float)timeSinceLastExecution.TotalSeconds;
                    HeadingAngle = 0;
                }
                else
                {
                    X += Speed * (float)timeSinceLastExecution.TotalSeconds;
                    HeadingAngle = 180;
                }
            }

            _lastExecutionTime = currentTime;
        }

        private void MoveCarOnCircle(float timeElapsed, bool rightCircleControll, Point circle)
        {
            //distanceToEndOfTheTrack = positionofcar - positionofBorder
            //float r = DistanceFromPointToPoint(X, Y, circle.X, circle.Y);
            float r = _perfectCircleRadius;
            // Wyznaczamy nowy kąt, uwzględniając czas i prędkość
            float a=0, b=0;
            if(rightCircleControll)
            {
                a= circle.X;
                b= circle.Y;
            }

            currentTurnAngle += Speed *timeElapsed/ r;
            HeadingAngle =-(float)((currentTurnAngle+Math.PI/2) * (180.0 / Math.PI));
            // Wyznaczamy nowe współrzędne X i Y samochodu
            X = a + r * (float)Math.Cos(-currentTurnAngle);
            Y = b + r * (float)Math.Sin(-currentTurnAngle);

            //return new Tuple<float, float>(x, y);
        }
        
        public void ConvertSpeedToVectors()
        {

        }
        public float DistanceFromPointToPoint(float x1,float y1, float x2, float y2)
        {
            return (float)Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));

        }
        //sila odsrodkowa
        public float CentrifugalForce(float speed, float radius, float mass)
        {
            return mass * (float)Math.Pow(speed, 2) * radius;
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
        private float Acceleration()
        {
            return CurrentHorsePower;
        }

        public float IscentrifugalForceEffectig()
        {
            float frictionDueToCentrifugalForce = CentrifugalForce(Speed, HeadingAngle, _mass) * (float)Math.Cos(trackAngle);
            float frictionAll = frictionDueToCentrifugalForce + FrictionForce();
            float centrifugalForce = CentrifugalForce(Speed, HeadingAngle, _mass);

            //nie ma pośligu
            if (frictionAll >= centrifugalForce)
            {
                return 0;
            }
            //jest poślizg
            else
            {
                return 1;
            }
        }
        private List<double> FindCircle(List<Point> points)
        {
            return FindCircle(points[0].X, points[0].Y, points[1].X, points[1].Y, points[2].X, points[2].Y);
        }
        private List<double> FindCircle(int x1, int y1,
                                        int x2, int y2,
                                        int x3, int y3)
        {
            double x12 = x1 - x2;
            double x13 = x1 - x3;

            double y12 = y1 - y2;
            double y13 = y1 - y3;

            double y31 = y3 - y1;
            double y21 = y2 - y1;

            double x31 = x3 - x1;
            double x21 = x2 - x1;

            // x1^2 - x3^2
            double sx13 = (int)(Math.Pow(x1, 2) -
                            Math.Pow(x3, 2));

            // y1^2 - y3^2
            double sy13 = (int)(Math.Pow(y1, 2) -
                            Math.Pow(y3, 2));

            double sx21 = (int)(Math.Pow(x2, 2) -
                            Math.Pow(x1, 2));

            double sy21 = (int)(Math.Pow(y2, 2) -
                            Math.Pow(y1, 2));

            double f = ((sx13) * (x12)
                    + (sy13) * (x12)
                    + (sx21) * (x13)
                    + (sy21) * (x13))
                    / (2 * ((y31) * (x12) - (y21) * (x13)));
            double g = ((sx13) * (y12)
                    + (sy13) * (y12)
                    + (sx21) * (y13)
                    + (sy21) * (y13))
                    / (2 * ((x31) * (y12) - (x21) * (y13)));

            double c = -Math.Pow(x1, 2) - Math.Pow(y1, 2) -
                                        2 * g * x1 - 2 * f * y1;

            // eqn of circle be x^2 + y^2 + 2*g*x + 2*f*y + c = 0
            // where centre is (h = -g, k = -f) and radius r
            // as r^2 = h^2 + k^2 - c
            double h = -g;
            double k = -f;
            double sqr_of_r = h * h + k * k - c;

            // r is the radius
            double r = Math.Round(Math.Sqrt(sqr_of_r), 5);

            List<double> result = new List<double>();
            result.Add(h);
            result.Add(k);
            result.Add(r);
            return result;
        }
    }
}