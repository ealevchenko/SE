using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;
using VRage.Game;
using VRage.Library;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Ingame;
using Sandbox.Common;
using Sandbox.Game;
using VRage.Collections;
using VRage.Game.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using Sandbox.Game.Entities;
using System.Runtime.CompilerServices;
using Sandbox.Game.GameSystems.Electricity;
using System.Reflection;

namespace Connection
{
    public sealed class Program : MyGridProgram
    {

        IMyTextPanel test_lcd;
        // Название 
        string NameGroup = "KLEPA1";
        string NameCockpit = "KLEPA1-Промышленный кокпит [LCD]";
        string NameConnector = "KLEPA1-Коннектор парковка";

        BatteryBlock batterys;
        Thrust thrust;
        ShipGrinder grinder;
        ShipWelder welder;
        ReflectorLight reflector;
        Gyro giros;
        Cockpit cocpit;
        Connector connector;


        bool old_parking = false;
        bool enable_horizont = false; // Бит держать горизонт
        int clock = 0;

        static Program _scr;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _scr = this;
            test_lcd = GridTerminalSystem.GetBlockWithName("test_lcd") as IMyTextPanel;
            Echo("test_lcd: " + ((test_lcd != null) ? ("Ок") : ("not found")));

            batterys = new BatteryBlock(NameGroup);
            thrust = new Thrust(NameGroup);
            grinder = new ShipGrinder(NameGroup);
            welder = new ShipWelder(NameGroup);
            reflector = new ReflectorLight(NameGroup);
            giros = new Gyro(NameGroup);

            cocpit = new Cockpit(NameCockpit);
            connector = new Connector(NameConnector);

            old_parking = connector.Connected; // Сохраним текущее состояние
            grinder.Off();
            welder.Off();
            reflector.Off();
        }

        public void Save()
        {

        }
        // Переключить батареи
        public void Main(string argument, UpdateType updateSource)
        {
            //test_lcd.WriteText("old:" + old_parking.ToString() + " connector:"+connector.IsParkingEnabled.ToString() + "\n" , true);  
            //test_lcd.WriteText("clock="+ clock.ToString() +"updateSource-" + updateSource + "\n", false);            
            switch (argument)
            {
                case "connected_on":
                    connector.Connect();
                    ConnectedOn();
                    break;
                case "connected_off":
                    connector.Disconnect();
                    ConnectedOff();
                    break;
                case "horizont_on":
                    giros.GyroOver(true);
                    enable_horizont = true;
                    break;
                case "horizont_off":
                    giros.GyroOver(false);
                    enable_horizont = false;
                    break;
                default:
                    break;
            }
            if (updateSource == UpdateType.Update10)
            {
                clock++;
                test_lcd.WriteText("clock=" + clock.ToString() + " old:" + old_parking.ToString() + " connector:" + connector.Status + "\n", false);
                if (!old_parking && connector.Connected)
                {
                    // Включили парковку
                    ConnectedOn();
                }
                if (old_parking && !connector.Connected)
                {
                    // Выключли парковку
                    ConnectedOff();
                }
                // Проверка кокпит под контроллем
                if (!connector.Connected && !cocpit.IsUnderControl)
                {
                    cocpit.Dampeners(true);
                    welder.Off();
                    grinder.Off();
                    reflector.Off();
                }
                // Держать горизонт
                if (!connector.Connected && enable_horizont)
                {
                    KeepHorizon();
                }
            }

            old_parking = connector.Status == MyShipConnectorStatus.Connected ? true : false;
        }
        public void KeepHorizon()
        {
            giros.SetGyro(cocpit.GetAxisHorizon());
        }
        public void ConnectedOn()
        {
            welder.Off();
            grinder.Off();
            reflector.Off();
            cocpit.Dampeners(false);
            batterys.Charger();
            thrust.Off();
        }
        public void ConnectedOff()
        {
            batterys.Auto();
            thrust.On();
            cocpit.Dampeners(true);
        }
        public class BatteryBlock
        {
            List<IMyBatteryBlock> batterylist = new List<IMyBatteryBlock>();
            public int Count { get { return batterylist.Count(); } }
            public BatteryBlock(string name_group)
            {

                _scr.GridTerminalSystem.GetBlocksOfType<IMyBatteryBlock>(batterylist, r => r.CustomName.Contains(name_group));
                _scr.Echo("batterylist: " + ((batterylist != null && batterylist.Count() > 0) ? ("Ок") : ("not batterylist")));
            }

            public void Charger()
            {
                foreach (IMyBatteryBlock batt in batterylist)
                {
                    batt.ChargeMode = ChargeMode.Recharge;
                }
            }
            public void Auto()
            {
                foreach (IMyBatteryBlock batt in batterylist)
                {
                    batt.ChargeMode = ChargeMode.Auto;
                }
            }
        }
        public class Cockpit
        {
            IMyShipController cockpit;
            public bool IsUnderControl { get { return cockpit.IsUnderControl; } }
            public Cockpit(string name)
            {
                cockpit = _scr.GridTerminalSystem.GetBlockWithName(name) as IMyShipController;
                _scr.Echo("cockpit: " + ((cockpit != null) ? ("Ок") : ("not found")));
            }
            public void Dampeners(bool on)
            {
                cockpit.DampenersOverride = on;
            }
            // Получить axis горизонта
            public Vector3D GetAxisHorizon()
            {
                Vector3D grav = Vector3D.Normalize(cockpit.GetNaturalGravity());
                Vector3D axis = grav.Cross(cockpit.WorldMatrix.Down);
                if (grav.Dot(cockpit.WorldMatrix.Down) < 0)
                {
                    axis = Vector3D.Normalize(axis);
                }
                return axis;
            }
        }
        public class Thrust
        {
            List<IMyThrust> thrustlist = new List<IMyThrust>();
            public int Count { get { return thrustlist.Count(); } }
            public Thrust(string name_group)
            {

                _scr.GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrustlist, r => r.CustomName.Contains(name_group));
                _scr.Echo("thrustlist: " + ((thrustlist != null && thrustlist.Count() > 0) ? ("Ок") : ("not thrustlist")));
            }

            public void On()
            {
                foreach (IMyThrust thrust in thrustlist)
                {
                    thrust.ApplyAction("OnOff_On");
                }
            }
            public void Off()
            {
                foreach (IMyThrust thrust in thrustlist)
                {
                    thrust.ApplyAction("OnOff_Off");
                }
            }
        }
        // Коннектор
        public class Connector
        {
            IMyShipConnector connector;
            public MyShipConnectorStatus Status { get { return connector.Status; } }
            public bool Connected { get { return connector.Status == MyShipConnectorStatus.Connected ? true : false; } }
            public bool Unconnected { get { return connector.Status == MyShipConnectorStatus.Unconnected ? true : false; } }
            public bool Connectable { get { return connector.Status == MyShipConnectorStatus.Connectable ? true : false; } }
            public Connector(string name)
            {
                connector = _scr.GridTerminalSystem.GetBlockWithName(name) as IMyShipConnector;
                _scr.Echo("connector: " + ((connector != null) ? ("Ок") : ("not found")));
            }
            public void Connect()
            {
                connector.Connect();
            }
            public void Disconnect()
            {
                connector.Disconnect();
            }
        }
        // Резаки
        public class ShipGrinder
        {
            List<IMyShipGrinder> grinderlist = new List<IMyShipGrinder>();
            public int Count { get { return grinderlist.Count(); } }
            public ShipGrinder(string name_group)
            {

                _scr.GridTerminalSystem.GetBlocksOfType<IMyShipGrinder>(grinderlist, r => r.CustomName.Contains(name_group));
                _scr.Echo("grinderlist: " + ((grinderlist != null && grinderlist.Count() > 0) ? ("Ок") : ("not grinderlist")));
            }

            public void On()
            {
                foreach (IMyShipGrinder grinder in grinderlist)
                {
                    grinder.ApplyAction("OnOff_On");
                }
            }
            public void Off()
            {
                foreach (IMyShipGrinder grinder in grinderlist)
                {
                    grinder.ApplyAction("OnOff_Off");
                }
            }
        }
        // сварщики
        public class ShipWelder
        {
            List<IMyShipWelder> welderlist = new List<IMyShipWelder>();
            public int Count { get { return welderlist.Count(); } }
            public ShipWelder(string name_group)
            {

                _scr.GridTerminalSystem.GetBlocksOfType<IMyShipWelder>(welderlist, r => r.CustomName.Contains(name_group));
                _scr.Echo("welderlist: " + ((welderlist != null && welderlist.Count() > 0) ? ("Ок") : ("not welderlist")));
            }

            public void On()
            {
                foreach (IMyShipWelder welder in welderlist)
                {
                    welder.ApplyAction("OnOff_On");
                }
            }
            public void Off()
            {
                foreach (IMyShipWelder welder in welderlist)
                {
                    welder.ApplyAction("OnOff_Off");
                }
            }
        }
        // Прожекторы
        public class ReflectorLight
        {
            List<IMyReflectorLight> reflectorlist = new List<IMyReflectorLight>();
            public int Count { get { return reflectorlist.Count(); } }
            public ReflectorLight(string name_group)
            {
                _scr.GridTerminalSystem.GetBlocksOfType<IMyReflectorLight>(reflectorlist, r => r.CustomName.Contains(name_group));
                _scr.Echo("reflectorlist: " + ((reflectorlist != null && reflectorlist.Count() > 0) ? ("Ок") : ("not reflectorlist")));
            }

            public void On()
            {
                foreach (IMyReflectorLight reflector in reflectorlist)
                {
                    reflector.ApplyAction("OnOff_On");
                }
            }
            public void Off()
            {
                foreach (IMyReflectorLight reflector in reflectorlist)
                {
                    reflector.ApplyAction("OnOff_Off");
                }
            }
        }
        // Гироскопы
        public class Gyro
        {
            List<IMyGyro> gyrolist = new List<IMyGyro>();
            public int Count { get { return gyrolist.Count(); } }
            public Gyro(string name_group)
            {
                _scr.GridTerminalSystem.GetBlocksOfType<IMyGyro>(gyrolist, r => r.CustomName.Contains(name_group));
                _scr.Echo("gyrolist: " + ((gyrolist != null && gyrolist.Count() > 0) ? ("Ок") : ("not gyrolist")));
            }
            public void SetGyro(Vector3D axis)
            {
                foreach (IMyGyro gyro in gyrolist)
                {
                    gyro.Yaw = (float)axis.Dot(gyro.WorldMatrix.Up);
                    gyro.Pitch = (float)axis.Dot(gyro.WorldMatrix.Right);
                    gyro.Roll = (float)axis.Dot(gyro.WorldMatrix.Backward);
                }
            }
            public void GyroOver(bool over)
            {
                foreach (IMyGyro gyro in gyrolist)
                {
                    gyro.Yaw = 0;
                    gyro.Pitch = 0;
                    gyro.Roll = 0;
                    gyro.GyroOverride = over;
                }
            }
        }
    }
}
