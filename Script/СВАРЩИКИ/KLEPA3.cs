using Sandbox.Definitions;
using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace KLEPA3
{
    public sealed class Program : MyGridProgram
    {
        //IMyTextPanel test_lcd;
        // Название 
        string NameGroup = "КЛЕПА3";
        string NameCockpit = "КЛЕПА3-Промышленный кокпит [LCD]";
        string NameConnector = "КЛЕПА3-Коннектор парковка";
        string NameLCD1 = "КЛЕПА3-ЖК-панель 1";

        BatteryBlock batterys;
        Thrust thrust;
        ReflectorLight reflector;
        Gyro giros;
        Cockpit cocpit;
        Connector connector;
        ShipWelder welder;
        TextPanel lcd1;


        bool old_parking = false;
        //int clock = 0;

        static Program _scr;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _scr = this;
            //test_lcd = GridTerminalSystem.GetBlockWithName("test_lcd") as IMyTextPanel;
            //Echo("test_lcd: " + ((test_lcd != null) ? ("Ок") : ("not found")));

            batterys = new BatteryBlock(NameGroup);
            thrust = new Thrust(NameGroup);
            reflector = new ReflectorLight(NameGroup);
            giros = new Gyro(NameGroup);
            welder = new ShipWelder(NameGroup); 
            cocpit = new Cockpit(NameCockpit);
            connector = new Connector(NameConnector);


            lcd1 = new TextPanel(NameLCD1);

            old_parking = connector.Connected; // Сохраним текущее состояние
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
                case "connected":
                    if (old_parking)
                    {
                        connector.Disconnect();
                        ConnectedOff();
                    }
                    else
                    {
                        connector.Connect();
                        ConnectedOn();
                    }
                    break;
                default:
                    break;
            }
            if (updateSource == UpdateType.Update10)
            {
                //clock++;
                //test_lcd.WriteText("clock=" + clock.ToString() + " old:" + old_parking.ToString() + " connector:" + connector.Status + "\n", false);
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
                    reflector.Off();
                }

            }
            // Информация
            lcd1.Write(connector.GetStatusOfText() + "\n", false, new Color(0, 255, 0));
            lcd1.Write(batterys.GetModeOfText() + "\n", true);

            old_parking = connector.Status == MyShipConnectorStatus.Connected ? true : false;
        }
        public void ConnectedOn()
        {
            reflector.Off();
            welder.Off();
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

            public string GetMode()
            {
                string result = "";
                foreach (IMyBatteryBlock batt in batterylist)
                {
                    result = batt.ChargeMode.ToString();
                }
                return result;
            }

            public string GetModeOfText()
            {
                string result = "БАТ:";
                foreach (IMyBatteryBlock batt in batterylist)
                {
                    switch (batt.ChargeMode)
                    {

                        case ChargeMode.Auto:
                            {
                                result += "А|";
                                break;
                            };
                        case ChargeMode.Recharge:
                            {
                                result += "З|";
                                break;
                            };
                        case ChargeMode.Discharge:
                            {
                                result += "Р|";
                                break;
                            };
                    }
                }
                return result;
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
                _scr.Echo(thrustlist[0].DefinitionDisplayNameText);
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
            public string GetStatusOfText()
            {
                string result = "КОН.:";

                switch (connector.Status)
                {

                    case MyShipConnectorStatus.Connected:
                        {
                            result += "Подкл.";
                            break;
                        };
                    case MyShipConnectorStatus.Unconnected:
                        {
                            result += "Готов";
                            break;
                        };
                    case MyShipConnectorStatus.Connectable:
                        {
                            result += "Откл.";
                            break;
                        };
                }

                return result;
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
        public class TextPanel
        {
            IMyTextPanel lcd;
            //IMyTextSurface lcd;
            public TextPanel(string name)
            {
                lcd = _scr.GridTerminalSystem.GetBlockWithName(name) as IMyTextPanel;
                _scr.Echo("lcd:[" + name + "]" + ((lcd != null) ? ("Ок") : ("not lcd")));
            }
            public bool Write(string text, bool app, Color col)
            {
                lcd.SetValueColor("FontColor", col);
                return lcd.WriteText(text, app);
            }
            public bool Write(string text, bool app)
            {
                return lcd.WriteText(text, app);
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

    }
}