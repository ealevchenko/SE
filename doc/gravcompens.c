
    //блок кабины и список блоков двигателей
    IMyCockpit Cockpit;
    List<IMyThrust> Thrusters;

    //находим нужные блоки
    public Program()
    {
        Cockpit = GridTerminalSystem.GetBlockWithName("[KROTIK_A1]-Промышленный кокпит [LCD]") as IMyCockpit;
        Thrusters = new List<IMyThrust>();
        GridTerminalSystem.GetBlocksOfType<IMyThrust>(Thrusters);
    }

    //в главной функции запускаем скрипт в рабочем режиме или останавливаем в зависимости от аргумента
    public void Main(string argument, UpdateType uType)
    {
        if (argument == "Compensate")
        {
            Cockpit.DampenersOverride = false;
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            CompensateWeight();
        }

        else if (argument == "Stop")
        {
            foreach (IMyThrust Thruster in Thrusters)
            {
                Thruster.ThrustOverridePercentage = 0f;
            }
            Cockpit.DampenersOverride = true;
            Runtime.UpdateFrequency = UpdateFrequency.None;
        }
        else if(uType==UpdateType.Update1)
        {
            CompensateWeight();
        }
    }

    //это рабочая процедура скрипта. 
    public void CompensateWeight()
    {

        Vector3D GravityVector = Cockpit.GetNaturalGravity();
        float ShipMass = Cockpit.CalculateShipMass().PhysicalMass;

        Vector3D ShipWeight = GravityVector * ShipMass;

        double ForwardThrust = ShipWeight.Dot(Cockpit.WorldMatrix.Forward);
        double LeftThrust = ShipWeight.Dot(Cockpit.WorldMatrix.Left);
        double UpThrust = ShipWeight.Dot(Cockpit.WorldMatrix.Up);
        //double DownThrust = -ShipWeight.Dot(Cockpit.WorldMatrix.Down);

        double BackwardThrust = -ForwardThrust;
        double RightThrust = -LeftThrust;
        double DownThrust = -UpThrust;



        Matrix CockpitMatrix = new MatrixD();
        Matrix ThrusterMatrix = new MatrixD();

        Cockpit.Orientation.GetMatrix(out CockpitMatrix);

        double UpThrMax = 0;
        double DownThrMax = 0;
        double LeftThrMax = 0;
        double RightThrMax = 0;
        double ForwardThrMax = 0;
        double BackwardThrMax = 0;

        foreach (IMyThrust Thruster in Thrusters)
        {
            Thruster.Orientation.GetMatrix(out ThrusterMatrix);
            //Y
            if (ThrusterMatrix.Forward == CockpitMatrix.Up)
            {
                UpThrMax += Thruster.MaxEffectiveThrust;
            }
            else if (ThrusterMatrix.Forward == CockpitMatrix.Down)
            {
                DownThrMax += Thruster.MaxEffectiveThrust;
            }
            //X
            else if (ThrusterMatrix.Forward == CockpitMatrix.Left)
            {
                LeftThrMax += Thruster.MaxEffectiveThrust;
            }
            else if (ThrusterMatrix.Forward == CockpitMatrix.Right)
            {
                RightThrMax += Thruster.MaxEffectiveThrust;
            }
            //Z
            else if (ThrusterMatrix.Forward == CockpitMatrix.Forward)
            {
                ForwardThrMax += Thruster.MaxEffectiveThrust;
            }
            else if (ThrusterMatrix.Forward == CockpitMatrix.Backward)
            {
                BackwardThrMax += Thruster.MaxEffectiveThrust;
            }
        }

        foreach (IMyThrust Thruster in Thrusters)
        {
            Thruster.Orientation.GetMatrix(out ThrusterMatrix);
            //Y
            if (ThrusterMatrix.Forward == CockpitMatrix.Up)
            {
                Thruster.ThrustOverridePercentage = (float)(UpThrust / UpThrMax);
            }
            else if (ThrusterMatrix.Forward == CockpitMatrix.Down)
            {
                Thruster.ThrustOverridePercentage = (float)(DownThrust / DownThrMax);
            }
            //X
            else if (ThrusterMatrix.Forward == CockpitMatrix.Left)
            {
                Thruster.ThrustOverridePercentage = (float)(LeftThrust / LeftThrMax);
            }
            else if (ThrusterMatrix.Forward == CockpitMatrix.Right)
            {
                Thruster.ThrustOverridePercentage = (float)(RightThrust / RightThrMax);
            }
            //Z
            else if (ThrusterMatrix.Forward == CockpitMatrix.Forward)
            {
                Thruster.ThrustOverridePercentage = (float)(ForwardThrust / ForwardThrMax);
            }
            else if (ThrusterMatrix.Forward == CockpitMatrix.Backward)
            {
                Thruster.ThrustOverridePercentage = (float)(BackwardThrust / BackwardThrMax);
            }
        }
    }
