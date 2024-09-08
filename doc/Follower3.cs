    string BotName = "BodyGuard";
    string OwnerName = "Pennywise";
    Follower Follower1;
    int StartCnt = 100;

    float koeffA = 5.0f;
    float koeffV = 5.0f;



    public Program()
    {
        Runtime.UpdateFrequency = UpdateFrequency.Update1;
    }

    public void Main(string argument)
    {
        StartCnt--;
        if (StartCnt == 0)
        {
            Follower1 = new Follower(this, BotName, OwnerName, koeffA, koeffV);
        }
        else if (StartCnt < 0)
        {
            //будем перемещаться к этой точке
            //GPS:Pennywise #1:-13010.66:50598.2:32509.12:
            //будем держать прицел на эту точку:
            //GPS:Pennywise #2:-12976.41:50487.77:32242.19:
            //Follower1.GoToPos(new Vector3D(-13010.66, 50598.2, 32509.12), new Vector3D(-12976.41, 50487.77, 32242.19));
            //Follower1.FollowMe();
            //Follower1.OrbitMe();
            //GPS: Pennywise #1:54478.96:-26989.72:7142:
            Follower1.OrbitAtackPos(new Vector3D(54478.96, -26989.72, 7142), 100, 500, 120);
        }

    }

    public class Follower
    {
        string MyName;
        string OwnerName;
        IMyRemoteControl RemCon;
        static Program ParentProgram;
        MyThrusters myThr;
        MyGyros myGyros;
        MySensors mySensors;
        MyWeapons myWeapons;
        float kV;
        float kA;

        public Follower(Program parenProg, string pref, string ownerN, float koeffA, float koeffV)
        {
            ParentProgram = parenProg;
            MyName = pref;
            kV = koeffV;
            kA = koeffA;
            OwnerName = ownerN;
            InitMainBlocks();
            InitSubSystems();
        }

        private void InitMainBlocks()
        {
            RemCon = ParentProgram.GridTerminalSystem.GetBlockWithName(MyName + "RemCon") as IMyRemoteControl;
        }

        private void InitSubSystems()
        {
            myThr = new MyThrusters(this);
            myGyros = new MyGyros(this, 20);
            mySensors = new MySensors(this);
            myWeapons = new MyWeapons(this);
        }

        public void DrawLocalVectors()
        {
            Vector3D LocX = new Vector3D(RemCon.WorldMatrix.M11, RemCon.WorldMatrix.M12, RemCon.WorldMatrix.M13) + RemCon.GetPosition();
            Vector3D LocY = new Vector3D(RemCon.WorldMatrix.M21, RemCon.WorldMatrix.M22, RemCon.WorldMatrix.M23) + RemCon.GetPosition();
            Vector3D LocZ = new Vector3D(RemCon.WorldMatrix.M31, RemCon.WorldMatrix.M32, RemCon.WorldMatrix.M33) + RemCon.GetPosition();
            IMyTextPanel TP = ParentProgram.GridTerminalSystem.GetBlockWithName(MyName + "TP") as IMyTextPanel;

            TP.WritePublicText("GPS:X:" + LocX.X + ":" + LocX.Y + ":" + LocX.Z + ":\n", false);
            TP.WritePublicText("GPS:Y:" + LocY.X + ":" + LocY.Y + ":" + LocY.Z + ":\n", true);
            TP.WritePublicText("GPS:Z:" + LocZ.X + ":" + LocZ.Y + ":" + LocZ.Z + ":\n", true);
        }

        //public void TestDrive(Vector3D Thr)
        //{
        //    myThr.SetThrA(Thr);
        //}

        public void TestHover()
        {
            Vector3D GravAccel = RemCon.GetNaturalGravity();
            //MatrixD MyMatrix = MatrixD.Invert(RemCon.WorldMatrix.GetOrientation());
            //myThr.SetThrA(Vector3D.Transform(-GravAccel, MyMatrix));

            MatrixD MyMatrix = RemCon.WorldMatrix.GetOrientation();
            myThr.SetThrA(VectorTransform(-GravAccel, MyMatrix));
        }


        public void FollowMe()
        {
            mySensors.UpdateSensors();
            if (mySensors.OwnerDetected)
            {
                GoToPos(mySensors.DetectedOwner.Position - RemCon.GetNaturalGravity());
            }
            else
            {
                //Здесь что-то надо делать, если потерян контакт с хозяином
            }
            if (mySensors.EnemyDetected)
            {
                Fire(mySensors.DetectedEnemy.Position);
            }
            else
            {
                //Здесь не обнаружен враг
            }
        }

        public void OrbitMe()
        {
            mySensors.UpdateSensors();
            if (mySensors.OwnerDetected)
            {
                OrbitPos(mySensors.DetectedOwner.Position, 14, 30, 25);
            }
            else
            {
                //Здесь что-то надо делать, если потерян контакт с хозяином
            }
            if (mySensors.EnemyDetected)
            {
                Fire(mySensors.DetectedEnemy.Position);
            }
            else
            {
                //Здесь не обнаружен враг
            }
        }

        public void OrbitAtackPos(Vector3D Pos, double OrbitH, double OrbitR, double OrbitV)
        {
            OrbitPos(Pos, OrbitH, OrbitR, OrbitV);
            if ((RemCon.GetPosition() - Pos).Length()<1000)
                Fire(Pos);
        }


        public void Fire(Vector3D Pos)
        {
            MatrixD MyMatrix = RemCon.WorldMatrix.GetOrientation();
            Vector3D A = RemCon.GetShipVelocities().LinearVelocity * (Pos - RemCon.GetPosition()).Length() / 800;
            if (myGyros.LookAtPoint(VectorTransform(Pos - RemCon.GetPosition() - A, MyMatrix)) < 0.1)
            {
                myWeapons.Fire();
            }
        }

        public void GoToPos(Vector3D Pos)
        {
            Vector3D GravAccel = RemCon.GetNaturalGravity();
            MatrixD MyMatrix = RemCon.WorldMatrix.GetOrientation();
            //Расчитать расстояние до цели
            Vector3D TargetVector = Pos - RemCon.GetPosition();
            Vector3D TargetVectorNorm = Vector3D.Normalize(TargetVector);
            //Расчитать желаемую скорость
            Vector3D DesiredVelocity = TargetVector * Math.Sqrt(2 * kV / TargetVector.Length());
            Vector3D VelocityDelta = DesiredVelocity - RemCon.GetShipVelocities().LinearVelocity;
            //Расчитать желаемое ускорение
            Vector3D DesiredAcceleration = VelocityDelta * kA;
            //Передаем желаемое ускорение с учетом гравитации движкам
            myThr.SetThrA(VectorTransform(DesiredAcceleration - GravAccel, MyMatrix));
        }

        public void OrbitPos(Vector3D Pos, double OrbitH, double OrbitR, double OrbitV)
        {
            //Получаем вертикальный вектор
            Vector3D GravAccel = RemCon.GetNaturalGravity();
            Vector3D GravAccelNorm = Vector3D.Normalize(GravAccel);
            MatrixD MyMatrix = RemCon.WorldMatrix.GetOrientation();
            //Расчитать вектор до цели
            Vector3D TargetVector = Pos - RemCon.GetPosition();
            Vector3D TargetVectorNorm = Vector3D.Normalize(TargetVector);
            //Расчитать горизонтальный вектор до цели
            Vector3D TargetVectorHor = Vector3D.Reject(TargetVector, GravAccelNorm);
            Vector3D TargetVectorHorNorm = Vector3D.Normalize(TargetVectorHor);
            //Расчитать горизонтальную координату на орбите
            Vector3D OrbitPosHor = Pos - TargetVectorHorNorm * OrbitR;
            Vector3D OrbitPos = OrbitPosHor - GravAccelNorm * OrbitH;
            /*
            //Если хотим высоту над ландшафтом
            double CurrentElevation = 0;
            double DesiredElevation = OrbitH;
            RemCon.TryGetPlanetElevation(MyPlanetElevation.Surface, out CurrentElevation);
            ParentProgram.Echo(CurrentElevation.ToString());
            OrbitPos = OrbitPosHor - GravAccelNorm * (DesiredElevation - CurrentElevation);
            */

            //Расчитать вектор до входной точки на орбите
            Vector3D OrbitVector = OrbitPos - RemCon.GetPosition();

            //Расчитать вектор скорости на орбите
            Vector3D OrbitVel = TargetVectorHorNorm.Cross(GravAccelNorm) * OrbitV;

            //Расчитать желаемую скорость
            Vector3D DesiredVelocity = OrbitVector * Math.Sqrt(2 * kV / OrbitVector.Length());

            if (OrbitVector.Length() < 1000)
                DesiredVelocity += OrbitVel;

            Vector3D VelocityDelta = DesiredVelocity - RemCon.GetShipVelocities().LinearVelocity;
            //Расчитать желаемое ускорение
            Vector3D DesiredAcceleration = VelocityDelta * kA;
            //Передаем желаемое ускорение с учетом гравитации движкам
            myThr.SetThrA(VectorTransform(DesiredAcceleration - GravAccel, MyMatrix));
        }

        public Vector3D VectorTransform(Vector3D Vec, MatrixD Orientation)
        {
            return new Vector3D(Vec.Dot(Orientation.Right), Vec.Dot(Orientation.Up), Vec.Dot(Orientation.Backward));
        }

        private class MyWeapons
        {
            Follower myBot;
            List<IMySmallMissileLauncher> Gatlings;
            public MyWeapons(Follower mbt)
            {
                myBot = mbt;
                InitMainBlocks();
            }

            private void InitMainBlocks()
            {
                Gatlings = new List<IMySmallMissileLauncher>();
                ParentProgram.GridTerminalSystem.GetBlocksOfType<IMySmallMissileLauncher>(Gatlings);
            }

            public void Fire()
            {
                foreach (IMySmallMissileLauncher gun in Gatlings)
                {
                    gun.ApplyAction("ShootOnce");
                }
            }

        }


        private class MySensors
        {
            Follower myBot;
            List<IMySensorBlock> Sensors;
            public List<MyDetectedEntityInfo> DetectedEntities;
            public MyDetectedEntityInfo DetectedOwner;
            public bool OwnerDetected;
            public MyDetectedEntityInfo DetectedEnemy;
            public bool EnemyDetected;


            public MySensors(Follower mbt)
            {
                myBot = mbt;
                InitMainBlocks();
            }

            private void InitMainBlocks()
            {
                Sensors = new List<IMySensorBlock>();
                DetectedEntities = new List<MyDetectedEntityInfo>();
                ParentProgram.GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(Sensors);
            }

            public void UpdateSensors()
            {
                OwnerDetected = false;
                EnemyDetected = false;
                foreach (IMySensorBlock sensor in Sensors)
                {
                    sensor.DetectedEntities(DetectedEntities);
                    foreach (MyDetectedEntityInfo detEnt in DetectedEntities)
                    {
                        if (detEnt.Name == myBot.OwnerName)
                        {
                            DetectedOwner = detEnt;
                            OwnerDetected = true;
                        }

                        if (detEnt.Relationship == MyRelationsBetweenPlayerAndBlock.Enemies)
                        {
                            DetectedEnemy = detEnt;
                            EnemyDetected = true;
                        }

                    }
                }
            }

        }

        private class MyGyros
        {
            List<IMyGyro> Gyros;
            float gyroMult;
            Follower myBot;

            public MyGyros(Follower mbt, float mult)
            {
                myBot = mbt;
                gyroMult = mult;
                InitMainBlocks();
            }

            private void InitMainBlocks()
            {
                Gyros = new List<IMyGyro>();
                ParentProgram.GridTerminalSystem.GetBlocksOfType<IMyGyro>(Gyros);
            }

            public float LookAtPoint(Vector3D LookPoint)
            {
                Vector3D SignalVector = Vector3D.Normalize(LookPoint);
                foreach (IMyGyro gyro in Gyros)
                {
                    gyro.Pitch = -(float)SignalVector.Y * gyroMult;
                    gyro.Yaw = (float)SignalVector.X * gyroMult;
                    gyro.Roll = 1f;
                }
                return (Math.Abs((float)SignalVector.Y) + Math.Abs((float)SignalVector.X));
            }

        }

        private class MyThrusters
        {
            Follower myBot;
            List<IMyThrust> AllThrusters;
            List<IMyThrust> UpThrusters;
            List<IMyThrust> DownThrusters;
            List<IMyThrust> LeftThrusters;
            List<IMyThrust> RightThrusters;
            List<IMyThrust> ForwardThrusters;
            List<IMyThrust> BackwardThrusters;

            double UpThrMax;
            double DownThrMax;
            double LeftThrMax;
            double RightThrMax;
            double ForwardThrMax;
            double BackwardThrMax;


            //переменные подсистемы двигателей
            public MyThrusters(Follower mbt)
            {
                myBot = mbt;
                InitMainBlocks();
            }

            private void InitMainBlocks()
            {
                Matrix ThrLocM = new Matrix();
                Matrix MainLocM = new Matrix();
                myBot.RemCon.Orientation.GetMatrix(out MainLocM);

                AllThrusters = new List<IMyThrust>();
                UpThrusters = new List<IMyThrust>();
                DownThrusters = new List<IMyThrust>();
                LeftThrusters = new List<IMyThrust>();
                RightThrusters = new List<IMyThrust>();
                ForwardThrusters = new List<IMyThrust>();
                BackwardThrusters = new List<IMyThrust>();
                UpThrMax = 0;
                DownThrMax = 0;
                LeftThrMax = 0;
                RightThrMax = 0;
                ForwardThrMax = 0;
                BackwardThrMax = 0;

                ParentProgram.GridTerminalSystem.GetBlocksOfType<IMyThrust>(AllThrusters);

                for (int i = 0; i < AllThrusters.Count; i++)
                {
                    IMyThrust Thrust = AllThrusters[i];
                    Thrust.Orientation.GetMatrix(out ThrLocM);
                    //Y
                    if (ThrLocM.Backward == MainLocM.Up)
                    {
                        UpThrusters.Add(Thrust);
                        UpThrMax += Thrust.MaxEffectiveThrust;
                    }
                    else if (ThrLocM.Backward == MainLocM.Down)
                    {
                        DownThrusters.Add(Thrust);
                        DownThrMax += Thrust.MaxEffectiveThrust;
                    }
                    //X
                    else if (ThrLocM.Backward == MainLocM.Left)
                    {
                        LeftThrusters.Add(Thrust);
                        LeftThrMax += Thrust.MaxEffectiveThrust;
                    }
                    else if (ThrLocM.Backward == MainLocM.Right)
                    {
                        RightThrusters.Add(Thrust);
                        RightThrMax += Thrust.MaxEffectiveThrust;
                    }
                    //Z
                    else if (ThrLocM.Backward == MainLocM.Forward)
                    {
                        ForwardThrusters.Add(Thrust);
                        ForwardThrMax += Thrust.MaxEffectiveThrust;
                    }
                    else if (ThrLocM.Backward == MainLocM.Backward)
                    {
                        BackwardThrusters.Add(Thrust);
                        BackwardThrMax += Thrust.MaxEffectiveThrust;
                    }
                }
            }
            private void SetGroupThrust(List<IMyThrust> ThrList, float Thr)
            {
                for (int i = 0; i < ThrList.Count; i++)
                {
                    //ThrList[i].SetValue("Override", Thr); //OldSchool
                    ThrList[i].ThrustOverridePercentage = Thr;
                }
            }
            public void SetThrF(Vector3D ThrVec)
            {
                SetGroupThrust(AllThrusters, 0f);
                //X
                if (ThrVec.X > 0)
                {
                    SetGroupThrust(RightThrusters, (float)(ThrVec.X / RightThrMax));
                }
                else
                {
                    SetGroupThrust(LeftThrusters, -(float)(ThrVec.X / LeftThrMax));
                }
                //Y
                if (ThrVec.Y > 0)
                {
                    SetGroupThrust(UpThrusters, (float)(ThrVec.Y / UpThrMax));
                }
                else
                {
                    SetGroupThrust(DownThrusters, -(float)(ThrVec.Y / DownThrMax));
                }
                //Z
                if (ThrVec.Z > 0)
                {
                    SetGroupThrust(BackwardThrusters, (float)(ThrVec.Z / BackwardThrMax));
                }
                else
                {
                    SetGroupThrust(ForwardThrusters, -(float)(ThrVec.Z / ForwardThrMax));
                }
            }
            public void SetThrA(Vector3D ThrVec)
            {
                double PhysMass = myBot.RemCon.CalculateShipMass().PhysicalMass;
                SetThrF(ThrVec * PhysMass);
            }


        }

    }