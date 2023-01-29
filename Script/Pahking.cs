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
using static pahking.Program;

namespace pahking
{
    public sealed class Program : MyGridProgram
    {

        IMyTextPanel test_lcd;
        // Название 
        string NameGroup = "KLEPA1";
        string NameCockpit = "Промышленный кокпит";
        string NameConnector = "Коннектор парковка";

        List<IMyTerminalBlock> list_block = new List<IMyTerminalBlock>();                  // Список всех блоков

        //IMyShipController cockpit;
        //IMyShipConnector connector;
        //List<IMyBatteryBlock> batterylist = new List<IMyBatteryBlock>();                // Батареи
        List<IMyThrust> thrustlist = new List<IMyThrust>();                             // трастеры

        bool old_parking = true;
        int clock = 0;

        static Program _scr;

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _scr = this;
            test_lcd = GridTerminalSystem.GetBlockWithName("test_lcd") as IMyTextPanel;




            //BatteryBlock batterys = new BatteryBlock(NameGroup);
            



            // Поиск объектов
            GridTerminalSystem.GetBlocksOfType<IMyTerminalBlock>(list_block, r => r.CustomName.Contains(NameGroup));
            foreach (IMyTerminalBlock obj in list_block)
            {
                //if (obj.CustomName.Contains(NameCockpit)) { cockpit = (IMyShipController)obj; }
                //if (obj.CustomName.Contains(NameConnector)) { connector = (IMyShipConnector)obj; }
                //if (obj is IMyBatteryBlock) { batterylist.Add((IMyBatteryBlock)obj); }                      // добавим все батареи
                //if (obj is IMyThrust) { thrustlist.Add((IMyThrust)obj); }                                   // добавим все трастеры
            }
            // Проверка объектов
            Echo("test_lcd: " + ((test_lcd != null) ? ("Ок") : ("not found")));

            //Echo("connector: " + ((connector != null) ? ("Ок") : ("not found")));

            //Echo("thrustlist: " + ((thrustlist != null && thrustlist.Count() > 0) ? ("Ок") : ("not thrustlist")));

            old_parking = connector.Status == MyShipConnectorStatus.Connected ? true : false; // Сохраним текущее состояние

        }

        public void Save()
        {

        }
        // Переключить батареи
        public void Main(string argument, UpdateType updateSource)
        {
            //test_lcd.WriteText("old:" + old_parking.ToString() + " connector:"+connector.IsParkingEnabled.ToString() + "\n" , true);  
            //test_lcd.WriteText("clock="+ clock.ToString() +"updateSource-" + updateSource + "\n", false);            
            if (updateSource == UpdateType.Update10)
            {
                clock++;
                test_lcd.WriteText("clock=" + clock.ToString() + " old:" + old_parking.ToString() + " connector:" + connector.Status + "\n", false);


                if (!old_parking && connector.Status == MyShipConnectorStatus.Connected)
                {
                    // Включили парковку
                    cockpit.DampenersOverride = false;
                    ChargeBatteries(true);
                    ThrustOnOff(false);
                }

                if (old_parking && connector.Status != MyShipConnectorStatus.Connected)
                {
                    // Выключли парковку
                    ChargeBatteries(false);
                    ThrustOnOff(true);
                    cockpit.DampenersOverride = true;
                }

            }
            else
            {

            }

            old_parking = connector.Status == MyShipConnectorStatus.Connected ? true : false;
        }

        public class BatteryBlock
        {
            List<IMyBatteryBlock> batterylist = new List<IMyBatteryBlock>();
            public int Count { get { return batterylist.Count(); } }
            public BatteryBlock(string name_group) {

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

        public class Cockpit {
            IMyCockpit cockpit;
            public Cockpit(string name) {
                cockpit = _scr.GridTerminalSystem.GetBlockWithName(name) as IMyCockpit;
                _scr.Echo("cockpit: " + ((cockpit != null) ? ("Ок") : ("not found")));
            }
        }

        public class Thrust
        {
            List<IMyThrust> thrustlist = new List<IMyThrust>();
            public int Count { get { return thrustlist.Count(); } }
            public Thrust(string name_group) {

                _scr.GridTerminalSystem.GetBlocksOfType<IMyThrust>(thrustlist, r => r.CustomName.Contains(name_group));
                _scr.Echo("thrustlist: " + ((thrustlist != null && thrustlist.Count() > 0) ? ("Ок") : ("not thrustlist")));
            }

            public void On()
            {
                foreach (IMyBatteryBlock thrust in thrustlist)
                {
                    thrust.ApplyAction("OnOff_On");
                }
            }
            public void Off()
            {
                foreach (IMyBatteryBlock thrust in thrustlist)
                {
                    thrust.ApplyAction("OnOff_Off");
                }
            }
        }

        public class Connector
        {
            IMyShipConnector connector;
            public MyShipConnectorStatus Status { get { return connector.Status; } }
            public bool Connected { get { return connector.Status == MyShipConnectorStatus.Connected ? true : false;} }
            public bool Unconnected { get { return connector.Status == MyShipConnectorStatus.Unconnected ? true : false;} }
            public bool Connectable { get { return connector.Status == MyShipConnectorStatus.Connectable ? true : false;} }
            public Connector(string name)
            {
                connector = _scr.GridTerminalSystem.GetBlockWithName(name) as IMyShipConnector;
                _scr.Echo("connector: " + ((connector != null) ? ("Ок") : ("not found")));
            }
        }
    }
}