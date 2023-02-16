using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.ModAPI.Ingame;
using VRageMath;
using static System.Net.Mime.MediaTypeNames;

namespace NASTYA_LOGIC_DOORS
{
    public sealed class Program : MyGridProgram
    {
        static IMyTextPanel test_lcd;//, test_lcd1;

        string test_name = "NASTYA1-info [door-info] [factory]";
        IMyTerminalBlock test;

        public static Color red = new Color(255, 0, 0);
        public static Color yellow = new Color(255, 255, 0);
        public static Color green = new Color(0, 128, 0);
        public enum room : int
        {
            none = 0,
            basement = 1,           // подвал
            factory = 2,            // завод
            hangar = 3,             // ангар
            habitation = 4,         // жилой модуль ()
            medical = 5,            // медицинский модуль
            captain = 6,            // капитанская каюта
            assistant = 7,          // каюта помошника
            cryo_left = 8,          // крео-камера
            cryo_right = 9,         // крео-камера
            canteen = 10,           // столовая
            cabin = 11,             // кабина
            operators = 12,         // операторская
            hydrogen_left_1 = 13,   // водородный склад
            energy_left_1 = 14,     // энергетический модуль
            hydrogen_right_1 = 15,  // водородный склад
            energy_right_1 = 16,    // энергетический модуль
            reactor = 17,           // реактор
            technical_1 = 18,       // технический этаж
            hydrogen_left_2 = 19,   // водородный склад
            energy_left_2 = 20,     // энергетический модуль
            hydrogen_right_2 = 21,  // водородный склад
            energy_right_2 = 22,    // энергетический модуль
            technical_2 = 23,       // технический этаж
            technical_3 = 24,       // технический этаж
            training_left = 25,     // тренеровочный зал
            cabins_left = 26,       // каюты тех персонала
            training_right = 27,    // тренеровочный зал
            cabins_right = 28,      // каюты тех персонала
            gateway = 29,           // шлюзовая
            gateway_left = 30,      // шлюз левый		           
            gateway_right = 31,     // шлюз правый
            gateway_stern = 32,     // шлюз корма
            out_space = 33,         // Космос


        };
        public static string[] name_room = { "", "Подвал", "Завод", "Ангар", "Жилой модуль", "Мед-блок", "Капитан", "Помошник", "КРЕО-камеры", "КРЕО-камеры", "Столовая",
            "Кабина", "Операторская", "Водородный склад", "Энерго-модуль", "Водородный склад", "Энерго-модуль", "Реакторная", "Тех-этаж 1",
            "Водородный склад", "Энерго-модуль", "Водородный склад", "Энерго-модуль", "Тех-этаж 2", "Тех-этаж 3", "Тренеровочный зал", "Каюты", "Тренеровочный зал", "Каюты", "Шлюз", "Шлюз левый", "Шлюз правый", "Шлюз корма", "Выход в космос"};
        public static int[] count_room = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, };

        public enum doors_gareways : int
        {
            hangar_factory = 0,
            technical_1_hangar = 1,
        }

        string tag_info_tablo = "[door-info]";
        string tag_door_gateway = "[door-gateway]";

        AirInfo air_info;
        Gateways gateways_doors;


        string NameObj = "NASTYA1";

        // door [door-gateway] [hangar_factory] [hangar]
        // sn [door-gateway] [hangar_factory] [hangar]
        // door [door-gateway] [hangar_factory] [factory]
        // sn [door-gateway] [hangar_factory] [factory]
        // door:    [door-gateway] [technical_1-hangar] [technical_1]
        // sn:      [door-gateway] [technical_1-hangar] [technical_1]
        // door:    [door-gateway] [technical_1-hangar] [hangar]
        // sn:      [door-gateway] [technical_1-hangar] [hangar]

        static Program _scr;

        static void DisplayBlockInfo(ref StringBuilder values, IMyTerminalBlock unit)
        {
            //        StringBuilder values = new StringBuilder();
            if (unit.CustomName.ToLower().Contains("skip") || unit.CustomData.ToLower().Contains("skip"))
            {
                //Echo("Skipping: " + unit.CustomName);
                return;
            }

            List<ITerminalAction> resultList = new List<ITerminalAction>();

            //Echo(unit.CustomName);
            unit.GetActions(resultList);

            values.Append(unit.CustomName + "\n");
            values.Append(unit.ToString() + "\n");
            values.Append(unit.EntityId.ToString() + "\n");

            values.Append("TyepID=" + unit.BlockDefinition.TypeIdString + "\n");
            values.Append("SubtyepID=" + unit.BlockDefinition.SubtypeId + "\n");
            values.Append("Mass=" + unit.Mass.ToString() + "\n");
            values.Append("IsBeingHacked =" + unit.IsBeingHacked.ToString() + "\n");
            values.Append("IsWorking =" + unit.IsWorking.ToString() + "\n");
            values.Append("IsFunctional =" + unit.IsFunctional.ToString() + "\n");
            values.Append("DisassembleRatio =" + unit.DisassembleRatio.ToString() + "\n");
            values.Append("DisplayNameText =" + unit.DisplayNameText.ToString() + "\n");
            values.Append("\nActions:\n");

            //Echo("#Results=" + resultList.Count.ToString());
            for (int i = 0; i < resultList.Count; i++)
            {
                StringBuilder temp = new StringBuilder();
                //        Echo(resultList[i].Id.ToString());
                //      Echo(resultList[i].Name.ToString());
                values.Append(resultList[i].Id + ":" + resultList[i].Name + "(");
                //      Echo(resultList[i].Id + ":" + resultList[i].Name + "(");
                if (resultList[i].Id.Length == 0)
                    resultList[i].WriteValue(unit, temp);
                else
                {
                    //            Echo(resultList[i].Id.ToString());
                    unit.GetActionWithName(resultList[i].Id.ToString()).WriteValue(unit, temp);
                }
                //        Echo(temp.ToString());
                values.Append(temp.ToString());
                values.Append(")\n");
            }
            //    Echo("AA1");
            List<ITerminalProperty> propList = new List<ITerminalProperty>();
            unit.GetProperties(propList);
            //    Echo("AA2");
            //Echo("#Properties=" + propList.Count.ToString());
            values.Append("\nProperties:\n");
            //    Echo("AA3");
            for (int i = 0; i < propList.Count; i++)
            {
                //        Echo(i + ":" + propList[i].TypeName);
                //        Echo("AA4:"+i.ToString());

                values.Append(propList[i].Id + ":" + propList[i].TypeName);
                //Echo(propList[i].Id + ":" + propList[i].TypeName);
                if (propList[i].TypeName == "Boolean")
                {
                    bool b = unit.GetValueBool(propList[i].Id);
                    values.Append(" (" + b + ")");
                }
                else if (propList[i].TypeName == "Single")
                {
                    float f = unit.GetValueFloat(propList[i].Id);
                    float fMax = unit.GetMaximum<float>(propList[i].Id);
                    float fMin = unit.GetMinimum<float>(propList[i].Id);
                    values.Append(" (" + f + ") Valid Range: " + fMin + "->" + fMax);
                }
                else if (propList[i].TypeName == "StringBuilder")
                {
                    string s = unit.GetValue<StringBuilder>(propList[i].Id).ToString();
                    values.Append(" (" + s + ")");
                }
                else if (propList[i].TypeName == "Int64")
                {
                    long l = unit.GetValue<long>(propList[i].Id);
                    long Max = unit.GetMaximum<long>(propList[i].Id);
                    long Min = unit.GetMinimum<long>(propList[i].Id);
                    values.Append(" (" + l + ") Valid Range: " + Min + "->" + Max);
                    //            values.Append(" (" + l + ")");
                }
                else if (propList[i].TypeName == "HashSet`1")
                { // from Cheetah's radar mod. http://steamcommunity.com/sharedfiles/filedetails/?id=907384096
                    HashSet<MyDetectedEntityInfo> hsInfo = unit.GetValue<HashSet<MyDetectedEntityInfo>>(propList[i].Id);
                    if (hsInfo == null)
                    {
                        // NavBall
                        //Echo("NOT deiinfo");
                    }
                    else
                    {
                        values.Append(" (" + hsInfo.Count + " Entries)");
                        //						if (hsInfo.Count > 0) values.AppendLine();
                        foreach (var myDei in hsInfo)//int i2=0;i2<hsInfo.Count; i2++)
                        {
                            if (myDei.IsEmpty())
                            {
                                values.AppendLine(); values.Append(" EMPTY!");
                            }
                            else
                            {
                                values.AppendLine(); values.Append(" Name: " + myDei.Name);
                                values.AppendLine(); values.Append(" Type: " + myDei.Type);
                                values.AppendLine(); values.Append(" RelationShip: " + myDei.Relationship);
                                //							values.AppendLine();		values.Append(" Size: " + myDei.BoundingBox.Size);
                                //							values.AppendLine();		values.Append(" Velocity: " + myDei.Velocity);
                                //							values.AppendLine();		values.Append(" Orientation: " + myDei.Orientation);
                                values.AppendLine(); values.Append(" ---");
                            }
                        }
                    }
                }

                values.AppendLine();
            }
            //Echo("End of Properties");

            values.Append("\nDetailedInfo:\n" + unit.DetailedInfo);
            values.Append("\n----------\n");

            values.Append("\nCustomInfo=" + unit.CustomInfo);
            values.Append("\nCustomName=" + unit.CustomName);
            values.Append("\nCustomData=" + unit.CustomData);
            values.Append("\nCustomNameWithFaction=" + unit.CustomNameWithFaction);
            values.Append("\nShowOnHUD=" + unit.ShowOnHUD);
            values.Append("\n");

            if (unit is IMyFunctionalBlock)
            {
                IMyFunctionalBlock ipp = unit as IMyFunctionalBlock;
                values.Append("\nIMyFunctionalBlock");
                values.Append("\n Enabled=" + ipp.Enabled.ToString());
                values.Append("\n");
            }
            //readded in 1.189 with different members
            if (unit is IMyPowerProducer)
            {
                IMyPowerProducer ipp = unit as IMyPowerProducer;

                values.Append("\nIMyPowerProducer");
                values.Append("\n CurrentOutput=" + ipp.CurrentOutput.ToString());
                //        values.Append("\n DefinedOutput=" + ipp.DefinedPowerOutput.ToString());
                values.Append("\n MaxOutput=" + ipp.MaxOutput.ToString());
                //        values.Append("\n ProductionEnabled=" + ipp.ProductionEnabled.ToString());
                values.Append("\n");
            }
            if (unit is IMyTextPanel)
            {
                values.Append("\nIMyTextPanel");
                values.Append("\n");
            }

            if (unit is IMyTextSurfaceProvider)
            {
                IMyTextSurfaceProvider ipp = unit as IMyTextSurfaceProvider;
                values.Append("\nIMyTextSurfaceProvider");
                values.Append("\n SurfaceCount=" + ipp.SurfaceCount.ToString());
                for (int i = 0; i < ipp.SurfaceCount; i++)
                {
                    IMyTextSurface ts = ipp.GetSurface(i);
                    values.Append("\n Surface " + i.ToString());
                    values.Append("\n  DisplayName=" + ts.DisplayName.ToString());
                    values.Append("\n  Name=" + ts.Name.ToString());
                    values.Append("\n  SurfaceSize=" + ts.SurfaceSize.ToString());
                    values.Append("\n  TextureSize=" + ts.TextureSize.ToString());

                }
                values.Append("\n");

            }
            if (unit is IMyBatteryBlock)
            {
                IMyBatteryBlock ipp = unit as IMyBatteryBlock;
                values.Append("\nIMyBatteryBlock");
                values.Append("\n CurrentStoredPower=" + ipp.CurrentStoredPower.ToString());
                values.Append("\n HasCapacityRemaining=" + ipp.HasCapacityRemaining.ToString());
                values.Append("\n MaxStoredPower=" + ipp.MaxStoredPower.ToString());
                values.Append("\n MaxOutput=" + ipp.MaxOutput.ToString());
                values.Append("\n ChargeMode=" + ipp.ChargeMode.ToString());
                /*
                values.Append("\n IsCharging=" + ipp.IsCharging.ToString());
                values.Append("\n OnlyDischarge=" + ipp.OnlyDischarge.ToString());
                values.Append("\n OnlyRecharge=" + ipp.OnlyRecharge.ToString());
                values.Append("\n SemiautoEnabled=" + ipp.SemiautoEnabled.ToString());
                */

                values.Append("\n");
            }
            if (unit is IMyGasTank)
            {
                IMyGasTank imgt = unit as IMyGasTank;
                values.Append("\nIMyGasTank");
                values.Append("\n AutoRefillBottles=" + imgt.AutoRefillBottles.ToString());
                values.Append("\n Capacity=" + imgt.Capacity.ToString());
                values.Append("\n FilledRatio=" + imgt.FilledRatio.ToString());
                values.Append("\n Stockpile=" + imgt.Stockpile.ToString());
                values.Append("\n");
            }
            if (unit is IMyThrust)
            {
                IMyThrust mythruster = unit as IMyThrust;
                values.Append("\nIMyThrust");
                values.Append("\n ThrustOverride=" + mythruster.ThrustOverride.ToString() + " newtons");
                values.Append("\n CurrentThrust=" + mythruster.CurrentThrust.ToString() + " newtons");
                values.Append("\n GridThrustDirection=" + mythruster.GridThrustDirection.ToString());
                values.Append("\n MaxEffectiveThrust=" + mythruster.MaxEffectiveThrust.ToString() + " newtons");
                values.Append("\n MaxThrust=" + mythruster.MaxThrust.ToString() + " newtons");
                values.Append("\n ThrustOverridePercentage=" + mythruster.ThrustOverridePercentage.ToString() + " 0->1 %");
                values.Append("\n");
            }
            if (unit is IMyReactor)
            {
                IMyReactor ipp = unit as IMyReactor;
                values.Append("\nIMyReactor");
                values.Append("\n CurrentOutput=" + ipp.CurrentOutput.ToString());
                values.Append("\n MaxOutput=" + ipp.MaxOutput.ToString());
                values.Append("\n");
            }
            if (unit is IMySolarPanel)
            {
                IMySolarPanel ipp = unit as IMySolarPanel;
                values.Append("\nIMySolarPanel");
                values.Append("\n CurrentOutput=" + ipp.CurrentOutput.ToString());
                values.Append("\n MaxOutput=" + ipp.MaxOutput.ToString());
                values.Append("\n");
            }

            if (unit is IMyParachute)
            {
                IMyParachute ipp = unit as IMyParachute;
                values.Append("\nIMyParachute");
                values.Append("\n Atmosphere=" + ipp.Atmosphere.ToString());
                values.Append("\n GetNaturalGravity()=" + ipp.GetNaturalGravity().ToString());
                values.Append("\n OpenRatio=" + ipp.OpenRatio.ToString());
                values.Append("\n Status=" + ipp.Status.ToString());
                values.Append("\n");
            }
            if (unit is IMyDoor)
            {
                IMyDoor imd = unit as IMyDoor;
                values.Append("\nIMyDoor ");
                values.Append("\n Status=" + imd.Status.ToString());
                values.Append("\n OpenRatio=" + imd.OpenRatio.ToString());
                values.Append("\n");
            }
            if (unit is IMyAdvancedDoor)
            {
                IMyAdvancedDoor imd = unit as IMyAdvancedDoor;
                values.Append("\nIMyAdvancedDoor ");
                values.Append("\n");
            }

            if (unit is IMyAirtightHangarDoor)
            {
                IMyAirtightHangarDoor imd = unit as IMyAirtightHangarDoor;
                values.Append("\nIMyAirtightHangarDoor ");
                values.Append("\n");
            }

            if (unit is IMyAirtightSlideDoor)
            {
                IMyAirtightSlideDoor imd = unit as IMyAirtightSlideDoor;
                values.Append("\nIMyAirtightSlideDoor ");
                values.Append("\n");
            }
            if (unit is IMyMotorSuspension)
            {
                IMyMotorSuspension ipp = unit as IMyMotorSuspension;
                values.Append("\nIMyMotorSuspension");
                values.Append("\n Brake=" + ipp.Brake.ToString());
                //        values.Append("\n Damping=" + ipp.Damping.ToString()); Removed 1.186
                values.Append("\n Friction=" + ipp.Friction.ToString());
                values.Append("\n Height=" + ipp.Height.ToString());
                values.Append("\n InvertSteer=" + ipp.InvertSteer.ToString());
                values.Append("\n MaxSteerAngle=" + ipp.MaxSteerAngle.ToString());
                values.Append("\n Power=" + ipp.Power.ToString());
                float maxPower = ipp.GetMaximum<float>("Power");
                values.Append("\n  MaxPower=" + maxPower.ToString());

                values.Append("\n Propulsion=" + ipp.Propulsion.ToString());
                values.Append("\n SteerAngle=" + ipp.SteerAngle.ToString());
                values.Append("\n Steering=" + ipp.Steering.ToString());
                //        values.Append("\n SteerReturnSpeed=" + ipp.SteerReturnSpeed.ToString()); Removed 1.186
                //        values.Append("\n SteerSpeed=" + ipp.SteerSpeed.ToString()); Removed 1.186
                values.Append("\n Strength=" + ipp.Strength.ToString());
                //        values.Append("\n SuspensionTravel=" + ipp.SuspensionTravel.ToString()); Removed 1.186
                values.Append("\n");
            }
            if (unit is IMyMotorStator)
            {
                IMyMotorStator ipp = unit as IMyMotorStator;
                values.Append("\nIMyMotorStator");
                values.Append("\n Angle=" + ipp.Angle.ToString());
                values.Append("\n BrakingTorque=" + ipp.BrakingTorque.ToString());
                values.Append("\n Displacement=" + ipp.Displacement.ToString());
                values.Append("\n IsAttached=" + ipp.IsAttached.ToString());
                //        values.Append("\n LowerLimit=" + ipp.LowerLimit.ToString());
                values.Append("\n Torque=" + ipp.Torque.ToString());
                //       values.Append("\n UpperLimit=" + ipp.UpperLimit.ToString());
                //        values.Append("\n Velocity=" + ipp.TargetVelocity.ToString());

                float minVelocity = ipp.GetMinimum<float>("Velocity");
                values.Append("\n  minVelocity=" + minVelocity.ToString());

                float maxVelocity = ipp.GetMaximum<float>("Velocity");
                values.Append("\n  maxVelocity=" + maxVelocity.ToString());


                values.Append("\n");
            }
            if (unit is IMyShipController)
            {
                IMyShipController ipp = unit as IMyShipController;
                values.Append("\nIMyShipController");
                values.Append("\n ControlThrusters =" + ipp.ControlThrusters.ToString());
                values.Append("\n ControlWheels =" + ipp.ControlWheels.ToString());
                values.Append("\n DampenersOverride =" + ipp.DampenersOverride.ToString());
                values.Append("\n HandBrake =" + ipp.HandBrake.ToString());
                values.Append("\n IsUnderControl =" + ipp.IsUnderControl.ToString());
                // lots more things available...
                values.Append("\n");
            }
            if (unit is IMyCockpit)
            {
                values.Append("\nIMyCockpit");
                IMyCockpit io = unit as IMyCockpit;
                MyShipMass myMass = io.CalculateShipMass();
                values.Append("\n OxygenCapacity=" + io.OxygenCapacity.ToString());
                values.Append("\n OxygenFilledRatio=" + io.OxygenFilledRatio.ToString());
                values.Append("\n");
            }
            if (unit is IMyCryoChamber)
            {
                values.Append("\nIMyCryoChamber");
                values.Append("\n");
            }

            if (unit is IMyRemoteControl)
            {
                values.Append("\nIMyRemoteControl");
                IMyRemoteControl rc = unit as IMyRemoteControl;
                //        MyWaypointInfo wpi = rc.CurrentWaypoint;
                //        values.Append("\n CurrentWaypoint=" + wpi.ToString());
                values.Append("\n FlightMode=" + rc.FlightMode.ToString());
                values.Append("\n IsAutoPilotEnabled=" + rc.IsAutoPilotEnabled.ToString());
                values.Append("\n SpeedLimit=" + rc.SpeedLimit.ToString());

                values.Append("\n");

            }
            if (unit is IMyAssembler)
            {
                IMyAssembler ipp = unit as IMyAssembler;
                values.Append("\nIMyAssembler");
                // OBSOLETE values.Append("\n DisassembleEnabled="+ipp.DisassembleEnabled.ToString()); 
                values.Append("\n Mode=" + ipp.Mode.ToString());
                values.Append("\n CoopMode=" + ipp.CooperativeMode.ToString());
                values.Append("\n CurrentProgress=" + ipp.CurrentProgress.ToString());
                values.Append("\n");
            }
            if (unit is IMyProductionBlock)
            {
                IMyProductionBlock ipp = unit as IMyProductionBlock;
                values.Append("\nIMyProductionBlock");
                values.Append("\n IsProducing=" + ipp.IsProducing.ToString());
                values.Append("\n IsQueueEmpty=" + ipp.IsQueueEmpty.ToString());
                values.Append("\n NextItemId=" + ipp.NextItemId.ToString());
                values.Append("\n UseConveyorSystem=" + ipp.UseConveyorSystem.ToString());
                values.Append("\n");
            }

            if (unit.HasInventory)
            {
                values.Append("\nHasInventory");
                //					values.Append("\n UseConveyorSystem="+unit.UseConveyorSystem.ToString());
                values.Append("\n InventoryCount=" + unit.InventoryCount.ToString());
                for (int i = 0; i < unit.InventoryCount; i++)
                {
                    IMyInventory inv = unit.GetInventory(i);
                    if (inv != null)
                    {
                        values.Append("\n\nIMyInventory[" + i + "]");

                        values.Append("\n CurrentMass=" + inv.CurrentMass.ToString());
                        values.Append("\n CurrentVolume=" + inv.CurrentVolume.ToString());
                        values.Append("\n IsFull=" + inv.IsFull.ToString());
                        values.Append("\n MaxVolume=" + inv.MaxVolume.ToString());
                    }
                }
                values.Append("\n");
            }
            else values.Append("\nNo Inventory\n");
            if (unit is IMyProjector)
            {
                values.Append("\nIMyProjector");

                IMyProjector io = unit as IMyProjector;
                /* OBSOLETE
                                    values.Append("\n ProjectionOffsetX="+io.ProjectionOffsetX.ToString());
                                    values.Append("\n ProjectionOffsetY="+io.ProjectionOffsetY.ToString());
                                    values.Append("\n ProjectionOffsetZ="+io.ProjectionOffsetZ.ToString());
                                    values.Append("\n ProjectionRotX="+io.ProjectionRotX.ToString());
                                    values.Append("\n ProjectionRotY="+io.ProjectionRotY.ToString());
                                    values.Append("\n ProjectionRotZ="+io.ProjectionRotZ.ToString());
                */
                values.Append("\n ProjectionOffset=" + io.ProjectionOffset.ToString());
                values.Append("\n ProjectionRotation=" + io.ProjectionRotation.ToString());
                values.Append("\n RemainingBlocks=" + io.RemainingBlocks.ToString());
                values.Append("\n");
            }
            if (unit is IMyGyro)
            {
                values.Append("\nIMyGyro");

                IMyGyro io = unit as IMyGyro;
                values.Append("\n GyroOverride=" + io.GyroOverride.ToString());
                values.Append("\n GyroPower=" + io.GyroPower.ToString());
                values.Append("\n Pitch=" + io.Pitch.ToString());
                values.Append("\n Roll=" + io.Roll.ToString());
                values.Append("\n Yaw=" + io.Yaw.ToString());
                values.Append("\n");
            }
            if (unit is IMyPistonBase)
            {
                values.Append("\nIMyPistonBase");

                IMyPistonBase io = unit as IMyPistonBase;
                values.Append("\n CurrentPosition=" + io.CurrentPosition.ToString());
                values.Append("\n MaxLimit=" + io.MaxLimit.ToString());
                values.Append("\n MinLimit=" + io.MinLimit.ToString());
                values.Append("\n Status=" + io.Status.ToString());
                values.Append("\n Velocity=" + io.Velocity.ToString());
                values.Append("\n");
            }
            if (unit is IMyHeatVent)
            {
                IMyHeatVent ihv = unit as IMyHeatVent;
                values.Append("\nIMyHeatVent ");
                values.Append("\n");
            }
            if (unit is IMyLargeGatlingTurret)
            {
                IMyLargeGatlingTurret ihv = unit as IMyLargeGatlingTurret;
                values.Append("\nIMyLargeGatlingTurret ");
                values.Append("\n");
            }
            if (unit is IMyTurretControlBlock)
            {
                IMyTurretControlBlock ihv = unit as IMyTurretControlBlock;
                values.Append("\nIMyTurretControlBlock ");
                values.Append("\n");
            }

            //Echo("Accepted resources:");
            values.Append("\nAccepted resources:");
            MyResourceSinkComponent sink;
            unit.Components.TryGet<MyResourceSinkComponent>(out sink);
            if (sink != null)
            {
                var list = sink.AcceptedResources;
                for (int j = 0; j < list.Count; ++j)
                {
                    values.Append("\n " + list[j].SubtypeId.ToString() + " (" + list[j].SubtypeName + ")");
                    //Echo(list[j].SubtypeId.ToString() + " (" + list[j].SubtypeName + ")");

                    float currentInput = 0;
                    float maxRequiredInput = 0;
                    bool isPoweredBy = false;

                    currentInput = sink.CurrentInputByType(list[j]);
                    isPoweredBy = sink.IsPoweredByType(list[j]);
                    maxRequiredInput = sink.MaxRequiredInputByType(list[j]);
                    //            float available = sink.ResourceAvailableByType(list[j]); // Prohibited


                    values.Append("\n Current=" + currentInput.ToString() + " Max=" + maxRequiredInput.ToString() + " PoweredBy=" + isPoweredBy.ToString()
                        //                + " Available=" +available.ToString()
                        );

                }
            }
            else
            {
                values.Append("\n No Resources");
                //Echo("No resources");
            }
            //Echo("Provided resources:");
            values.Append("\nProvided resources:");
            MyResourceSourceComponent source;
            unit.Components.TryGet<MyResourceSourceComponent>(out source);

            if (source != null)
            {
                /*
                float currentOutput = 0;
                float maxOutput = 0;
                currentOutput = source.CurrentOutput;
                maxOutput = source.MaxOutput;
                values.Append("\n Current=" + currentOutput.ToString() + " Max=" + maxOutput.ToString());
                */
                var list = source.ResourceTypes;
                for (int j = 0; j < list.Count; ++j)
                {
                    values.Append("\n " + /*list[j].TypeId.ToString()+"/" +*/ list[j].SubtypeId.ToString());
                    //Echo(list[j].SubtypeId.ToString());
                    float maxoutput = source.DefinedOutputByType(list[j]);
                    float currentoutput = source.CurrentOutputByType(list[j]);
                    values.Append(" Current=" + currentoutput + " Max=" + maxoutput);
                }
            }
            else
            {
                values.Append("\n No Resources");
                //Echo("No resources");
            }
        }
        public class PText
        {
            static public string GetPersent(double perse)
            {
                return " - " + Math.Round((perse * 100), 1) + "%";
            }
            static public string GetScalePersent(double perse, int scale)
            {
                string prog = "[";
                for (int i = 0; i < Math.Round((perse * scale), 0); i++)
                {
                    prog += "|";
                }
                for (int i = 0; i < scale - Math.Round((perse * scale), 0); i++)
                {
                    prog += "'";
                }
                prog += "]" + GetPersent(perse);
                return prog;
            }
            static public string GetCurrentOfMax(float cur, float max, string units)
            {
                return "[ " + cur + units + " / " + max + units + " ]";
            }
            static public string GetSign(int x, int y)
            {
                if (x == y) { return "="; }
                if (x > y) { return ">"; }
                if (x < y) { return "<"; }
                return "";
            }
            static public string GetCountObj(int count, int count_on)
            {
                return count_on + GetSign(count_on, count) + count;
            }
            //------------------------------------------------------------------------
            static public string GetCapacityTanks(double perse, float capacity)
            {
                return "[ " + Math.Round(((perse * capacity) / 1000000), 1) + "МЛ / " + Math.Round((capacity / 1000000), 1) + "МЛ ]";
            }
            static public string GetThrust(float value)
            {
                return Math.Round(value / 1000000, 1) + "МН";
            }
            static public string GetCurrentThrust(float to, float cur, float max)
            {
                return "[ " + GetThrust(to) + " / " + GetThrust(cur) + " / " + GetThrust(max) + " ]";
            }
            static public string GetCurrentThrust(float cur, float max)
            {
                return "[ " + GetThrust(cur) + " / " + GetThrust(max) + " ]";
            }
            static public string GetCountDetaliThrust(int count_a, int count_h, int count_i, int count_on_a, int count_on_h, int count_on_i)
            {
                string result = "[";
                if (count_a > 0)
                {
                    result += "{А-" + count_on_a + "|" + count_a + "}";
                }
                if (count_h > 0)
                {
                    result += "{В-" + count_on_h + "|" + count_h + "}";
                }
                if (count_i > 0)
                {
                    result += "{И-" + count_on_i + "|" + count_i + "}";
                }
                result += "]";
                return result;
            }
        }
        public class BaseListTerminalBlock<T> where T : class
        {
            public List<T> list_obj = new List<T>();
            public int Count { get { return list_obj.Count(); } }
            public BaseListTerminalBlock(string name_obj)
            {
                _scr.GridTerminalSystem.GetBlocksOfType<T>(list_obj, r => ((IMyTerminalBlock)r).CustomName.Contains(name_obj));
                _scr.Echo(typeof(T).Name + "[" + name_obj + "]" + ((list_obj != null && list_obj.Count() > 0) ? ("Ок") : ("not found"))); ;
            }
            public class values_obj
            {
                public int id_group = 0;
                public string TyepID = null;
                public string SubtyepID = null;
                public string definition_display_name_text = null;
                public int count = 0;                       // кол
                public int count_on = 0;                    // кол вкл

                public float curr_mass = 0;
                public float curr_vol = 0;
                public float curr_max_vol = 0;

                public float inp_curr_power = 0;
                public float inp_max_power = 0;
                public int count_inp_power = 0;

                public float inp_curr_hydrogen = 0;
                public float inp_max_hydrogen = 0;
                public int count_inp_hydrogen = 0;

                public float inp_curr_oxygen = 0;
                public float inp_max_oxygen = 0;
                public int count_inp_oxygen = 0;

                public float out_curr_power = 0;
                public float out_max_power = 0;

                public float out_curr_hydrogen = 0;
                public float out_max_hydrogen = 0;

                public float out_curr_oxygen = 0;
                public float out_max_oxygen = 0;
            }
            //
            public List<values_obj> list_values = new List<values_obj>();

            public void GetValues()
            {
                list_values.Clear();
                //_scr.test_lcd1.WriteText("Старт" + "\n", false);
                foreach (IMyTerminalBlock obj in list_obj)
                {
                    float curr_mass = 0;
                    float curr_vol = 0;
                    float curr_max_vol = 0;

                    float inp_curr_power = 0;
                    float inp_max_power = 0;
                    bool is_inp_power = false;

                    float inp_curr_hydrogen = 0;
                    float inp_max_hydrogen = 0;
                    bool is_inp_hydrogen = false;

                    float inp_curr_oxygen = 0;
                    float inp_max_oxygen = 0;
                    bool is_inp_oxygen = false;

                    float out_curr_power = 0;
                    float out_max_power = 0;

                    float out_curr_hydrogen = 0;
                    float out_max_hydrogen = 0;

                    float out_curr_oxygen = 0;
                    float out_max_oxygen = 0;

                    // Инвентарь
                    if (((IMyTerminalBlock)obj).HasInventory)
                    {
                        for (int i = 0; i < ((IMyTerminalBlock)obj).InventoryCount; i++)
                        {
                            IMyInventory inv = ((IMyTerminalBlock)obj).GetInventory(i);
                            if (inv != null)
                            {
                                curr_mass += ((float)inv.CurrentMass);
                                curr_vol += ((float)inv.CurrentVolume);
                                curr_max_vol += ((float)inv.MaxVolume);
                            }
                        }
                    }
                    //
                    MyResourceSinkComponent sink;
                    ((IMyTerminalBlock)obj).Components.TryGet<MyResourceSinkComponent>(out sink);
                    if (sink != null)
                    {
                        var list = sink.AcceptedResources;
                        for (int j = 0; j < list.Count; ++j)
                        {
                            if (list[j].SubtypeId.ToString() == "Electricity")
                            {
                                inp_curr_power += sink.CurrentInputByType(list[j]);
                                inp_max_power += sink.MaxRequiredInputByType(list[j]);
                                is_inp_power = sink.IsPoweredByType(list[j]);
                            }
                            if (list[j].SubtypeId.ToString() == "Hydrogen")
                            {
                                inp_curr_hydrogen += sink.CurrentInputByType(list[j]);
                                inp_max_hydrogen += sink.MaxRequiredInputByType(list[j]);
                                is_inp_hydrogen = sink.IsPoweredByType(list[j]);
                            }
                            if (list[j].SubtypeId.ToString() == "Oxygen")
                            {
                                inp_curr_oxygen += sink.CurrentInputByType(list[j]);
                                inp_max_oxygen += sink.MaxRequiredInputByType(list[j]);
                                is_inp_oxygen = sink.IsPoweredByType(list[j]);
                            }
                        }
                    }
                    MyResourceSourceComponent source;
                    ((IMyTerminalBlock)obj).Components.TryGet<MyResourceSourceComponent>(out source);
                    if (source != null)
                    {
                        var list = source.ResourceTypes;
                        for (int j = 0; j < list.Count; ++j)
                        {
                            if (list[j].SubtypeId.ToString() == "Electricity")
                            {
                                out_curr_power = source.CurrentOutputByType(list[j]);
                                out_max_power = source.DefinedOutputByType(list[j]);
                            }
                            if (list[j].SubtypeId.ToString() == "Oxygen")
                            {
                                out_curr_oxygen = source.CurrentOutputByType(list[j]);
                                out_max_oxygen = source.DefinedOutputByType(list[j]);
                            }
                            if (list[j].SubtypeId.ToString() == "Hydrogen")
                            {
                                out_curr_hydrogen = source.CurrentOutputByType(list[j]);
                                out_max_hydrogen = source.DefinedOutputByType(list[j]);
                            }
                        }
                    }

                    values_obj val_obj = list_values.Where(o => ((values_obj)o).TyepID == obj.BlockDefinition.TypeId.ToString() && ((values_obj)o).SubtyepID == obj.BlockDefinition.SubtypeId).FirstOrDefault();
                    if (val_obj == null)
                    {
                        val_obj = new values_obj()
                        {
                            id_group = 0,
                            definition_display_name_text = obj.DefinitionDisplayNameText,
                            TyepID = obj.BlockDefinition.TypeId.ToString(),
                            SubtyepID = obj.BlockDefinition.SubtypeId,
                            count = 1,
                            count_on = ((IMyFunctionalBlock)obj).Enabled ? 1 : 0,
                            curr_mass = curr_mass,
                            curr_vol = curr_vol,
                            curr_max_vol = curr_max_vol,
                            inp_curr_power = inp_curr_power,
                            inp_max_power = inp_max_power,
                            count_inp_power = is_inp_power ? 1 : 0,
                            inp_curr_hydrogen = inp_curr_hydrogen,
                            inp_max_hydrogen = inp_max_hydrogen,
                            count_inp_hydrogen = is_inp_hydrogen ? 1 : 0,
                            inp_curr_oxygen = inp_curr_oxygen,
                            inp_max_oxygen = inp_max_oxygen,
                            count_inp_oxygen = is_inp_oxygen ? 1 : 0,
                            out_curr_power = out_curr_power,
                            out_max_power = out_max_power,
                            out_curr_hydrogen = out_curr_hydrogen,
                            out_max_hydrogen = out_max_hydrogen,
                            out_curr_oxygen = out_curr_oxygen,
                            out_max_oxygen = out_max_oxygen,

                        };
                        list_values.Add(val_obj);
                    }
                    else
                    {
                        val_obj.count++;
                        if (((IMyFunctionalBlock)obj).Enabled) val_obj.count_on++;
                        val_obj.curr_mass = curr_mass;
                        val_obj.curr_vol = curr_vol;
                        val_obj.curr_max_vol = curr_max_vol;
                        val_obj.inp_curr_power = inp_curr_power;
                        val_obj.inp_max_power = inp_max_power;
                        if (is_inp_power) val_obj.count_inp_power++;
                        val_obj.inp_curr_hydrogen = inp_curr_hydrogen;
                        val_obj.inp_max_hydrogen = inp_max_hydrogen;
                        if (is_inp_hydrogen) val_obj.count_inp_hydrogen++;
                        val_obj.inp_curr_oxygen = inp_curr_oxygen;
                        val_obj.inp_max_oxygen = inp_max_oxygen;
                        if (is_inp_oxygen) val_obj.count_inp_oxygen++;
                        val_obj.out_curr_power = out_curr_power;
                        val_obj.out_max_power = out_max_power;
                        val_obj.out_curr_hydrogen = out_curr_hydrogen;
                        val_obj.out_max_hydrogen = out_max_hydrogen;
                        val_obj.out_curr_oxygen = out_curr_oxygen;
                        val_obj.out_max_oxygen = out_max_oxygen;
                    }
                }
            }
            // Команды включения\выключения
            private void Off(List<T> list)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    obj.ApplyAction("OnOff_Off");
                }
            }
            public void Off()
            {
                Off(list_obj);
            }
            private void OffOfGroup(List<T> list, string group)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    if (obj.CustomName.Contains(group))
                    {
                        obj.ApplyAction("OnOff_Off");
                    }
                }
            }
            public void OffOfGroup(string group)
            {
                OffOfGroup(list_obj, group);
            }
            private void On(List<T> list)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    obj.ApplyAction("OnOff_On");
                }
            }
            public void On()
            {
                On(list_obj);
            }
            private void OnOfGroup(List<T> list, string group)
            {
                foreach (IMyTerminalBlock obj in list)
                {
                    if (obj.CustomName.Contains(group))
                    {
                        obj.ApplyAction("OnOff_On");
                    }
                }
            }
            public void OnOfGroup(string group)
            {
                OnOfGroup(list_obj, group);
            }
        }
        public class BaseTerminalBlock<T> where T : class
        {
            public T obj;
            public BaseTerminalBlock(string name)
            {
                obj = _scr.GridTerminalSystem.GetBlockWithName(name) as T;
                _scr.Echo("block:[" + name + "]: " + ((obj != null) ? ("Ок") : ("not Block")));
            }
            public BaseTerminalBlock(T myobj)
            {
                obj = myobj;
                _scr.Echo("block:[" + obj.ToString() + "]: " + ((obj != null) ? ("Ок") : ("not Block")));
            }
            // Команды включения\выключения
            public void Off()
            {
                if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_Off");
            }
            public void On()
            {
                if (obj != null) ((IMyTerminalBlock)obj).ApplyAction("OnOff_On");
            }
        }
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _scr = this;

            test = GridTerminalSystem.GetBlockWithName(test_name) as IMyTerminalBlock;

            // тест LCD
            test_lcd = GridTerminalSystem.GetBlockWithName("NASTYA1-test_lcd") as IMyTextPanel;
            Echo("test_lcd: " + ((test_lcd != null) ? ("Ок") : ("not found")));
            //test_lcd1 = GridTerminalSystem.GetBlockWithName("NASTYA1-test_lcd1") as IMyTextPanel;
            //Echo("test_lcd: " + ((test_lcd != null) ? ("Ок") : ("not found")));
            //door_gataway_hangar_factory = new DoorGateway(dg_option_hangar_factory);
            air_info = new AirInfo(NameObj, tag_info_tablo);
            gateways_doors = new Gateways(NameObj, tag_door_gateway);
        }
        public void Save()
        {

        }
        public void Main(string argument, UpdateType updateSource)
        {
            //test_lcd.WriteText("old:" + old_parking.ToString() + " connector:"+connector.IsParkingEnabled.ToString() + "\n" , true);  
            //test_lcd.WriteText("clock="+ clock.ToString() +"updateSource-" + updateSource + "\n", false);            

            switch (argument)
            {
                //case "connected_on":
                //    break;
                default:
                    break;
            }
            if (updateSource == UpdateType.Update10)
            {
                //StringBuilder values = new StringBuilder();
                //DisplayBlockInfo(ref values, test);
                //test_lcd.WriteText(values, false);
                // Получим данные
                test_lcd.WriteText("" + "\n", false);
                // Логика отображения подписей двирей с учетом кислорода в помещении
                air_info.Logic(argument, updateSource);
                // Логика отработки шлюзовых дверей
                gateways_doors.Logic(argument, updateSource);
                test_lcd.WriteText("" + "\n", false);
                test_lcd.WriteText("hangar:" + count_room[(int)room.hangar] + "\n", true);
                test_lcd.WriteText("factory:" + count_room[(int)room.factory] + "\n", true);
                test_lcd.WriteText("technical_1:" + count_room[(int)room.technical_1] + "\n", true);
            }
        }
        public class Gateway
        {
            doors_gareways door_gtw;
            IMySensorBlock sn1;
            IMySensorBlock sn2;
            room rm1;
            IMyDoor door1;
            IMyDoor door2;
            room rm2;
            //bool input_door = false;
            //bool output_door = false;
            bool sn1_active = false;    // датчик входа
            bool sn2_active = false;   // датчик выхода
            public Gateway(doors_gareways dg, IMyDoor door1, IMySensorBlock sn1, room rm1, IMyDoor door2, IMySensorBlock sn2, room rm2)
            {
                this.door_gtw = dg;
                this.rm1 = rm1;
                this.rm2 = rm2;
                this.sn1 = sn1;
                string sn1_cd = sn1.CustomData; // 1.0f, 1.0f, 2.5f, 1.0f, 0.1f, 2.5f
                this.sn2 = sn2;
                this.door1 = door1;
                this.door2 = door2;
                this.door1.CloseDoor();
                this.door2.CloseDoor();
            }

            public void Logic()
            {
                if (!sn1.IsActive && door1.Status == DoorStatus.Open)
                {
                    // Игрок не найден возле внутр двери
                    door1.CloseDoor();
                }
                if (sn1.IsActive && door1.Status == DoorStatus.Closed && door2.Status == DoorStatus.Closed)
                {
                    // Игрокнайден возле внутр дверь закрыта и внешняя закрыта
                    door1.OpenDoor();
                }
                if (!sn2.IsActive && door2.Status == DoorStatus.Open)
                {
                    // Игрок не найден возле внутр двери
                    door2.CloseDoor();
                }
                if (sn2.IsActive && door2.Status == DoorStatus.Closed && door1.Status == DoorStatus.Closed)
                {
                    // Игрокнайден возле внутр дверь закрыта и внешняя закрыта
                    door2.OpenDoor();
                }
                // Логика направдения движения
                if (sn1_active && !sn2_active && sn2.IsActive)
                {
                    // Выход
                    //sn1_active = false;
                    sn2_active = true;
                    //input_door = false;
                    //output_door = true;
                    count_room[(int)rm1]--;
                    count_room[(int)rm2]++;
                }
                if (sn2_active && !sn1_active && sn1.IsActive)
                {
                    // Вход
                    sn1_active = true;
                    //sn2_active = false;
                    //input_door = true;
                    //output_door = false;
                    count_room[(int)rm1]++;
                    count_room[(int)rm2]--;
                }
                if (sn2_active && sn1_active && !sn2.IsActive && !sn1.IsActive)
                {
                    // Вход
                    sn1_active = false;
                    sn2_active = false;
                    //input_door = false;
                    //output_door = false;
                }

                if (!sn1_active && !sn2_active)
                {
                    // Выход
                    sn1_active = sn1.IsActive;
                    sn2_active = sn2.IsActive;
                }
                if (count_room[(int)rm1] < 0) count_room[(int)rm1] = 0;
                if (count_room[(int)rm2] < 0) count_room[(int)rm2] = 0;
            }
        }
        // Класс управления шлюзовыми дверями дверями 
        public class Gateways
        {
            private List<IMyDoor> doors = new List<IMyDoor>();
            private List<IMySensorBlock> sensors = new List<IMySensorBlock>();
            List<Gateway> list_gtw = new List<Gateway>();
            public Gateways(string name_obj, string tag)
            {
                //test_lcd.WriteText("Start" + "\n", false);
                _scr.GridTerminalSystem.GetBlocksOfType<IMyDoor>(doors, r => r.CustomName.Contains(name_obj) && r.CustomName.Contains(tag));
                _scr.GridTerminalSystem.GetBlocksOfType<IMySensorBlock>(sensors, r => r.CustomName.Contains(name_obj) && r.CustomName.Contains(tag));
                //test_lcd.WriteText("doors:" + doors.Count() + "\n", true);
                //test_lcd.WriteText("sensors:" + doors.Count() + "\n", true);
                IMyDoor door1;
                IMySensorBlock sensor1;
                room room1;
                IMyDoor door2;
                IMySensorBlock sensor2;
                room room2;
                //test_lcd.WriteText("Cikl" + "\n", false);
                foreach (doors_gareways gw in Enum.GetValues(typeof(doors_gareways)))
                {
                    door1 = null;
                    sensor1 = null;
                    room1 = room.none;
                    door2 = null;
                    sensor2 = null;
                    room2 = room.none;

                    List<IMyDoor> l_drs = doors.Where(d => d.CustomName.Contains("[" + gw.ToString() + "]")).ToList();
                    List<IMySensorBlock> l_sns = sensors.Where(d => d.CustomName.Contains("[" + gw.ToString() + "]")).ToList();
                    if (l_drs != null && l_drs.Count() == 2 && l_sns != null && l_sns.Count() == 2)
                    {
                        foreach (room rm in Enum.GetValues(typeof(room)))
                        {
                            //test_lcd.WriteText("room:" + rm + "\n", true);
                            IMyDoor dr = l_drs.Where(d => d.CustomName.Contains("[" + rm.ToString() + "]")).FirstOrDefault();
                            IMySensorBlock sn = l_sns.Where(d => d.CustomName.Contains("[" + rm.ToString() + "]")).FirstOrDefault();
                            if (dr != null && sn != null)
                            {
                                if (door1 != null && door2 == null) { door2 = dr; room2 = rm; }
                                if (door1 == null) { door1 = dr; room1 = rm; }
                                if (sensor1 != null && sensor2 == null) { sensor2 = sn; }
                                if (sensor1 == null) { sensor1 = sn; }
                            }
                        }
                        if (door1 != null && door2 != null && sensor1 != null && sensor2 != null)
                        {
                            //test_lcd.WriteText("door1:"+ door1.CustomName + "\n", true);
                            //test_lcd.WriteText("door2:"+ door2.CustomName + "\n", true);
                            //test_lcd.WriteText("sensor1:" + sensor1.CustomName + "\n", true);
                            //test_lcd.WriteText("sensor2:" + sensor2.CustomName + "\n", true);
                            list_gtw.Add(new Gateway(gw, door1, sensor1, room1, door2, sensor2, room2));
                        }
                    }

                }
            }

            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    //case "connected_on":
                    //    break;
                    default:
                        break;
                }

                if (updateSource == UpdateType.Update10)
                {
                    foreach (Gateway gateway in list_gtw)
                    {
                        gateway.Logic();
                    }
                }

            }
        }
        // Информационые табло дверей
        public class InfoTablo : BaseListTerminalBlock<IMyTextPanel>
        {
            string tag;
            List<IMyTextPanel> list = new List<IMyTextPanel>();
            public InfoTablo(string name_obj, string tag) : base(name_obj)
            {
                this.tag = tag;
                list = list_obj.Where(x => x.CustomName.Contains(this.tag)).ToList();
            }
            public void InitPanel()
            {
                // Пройдемся по помещениям и настроим панели
                foreach (room group in Enum.GetValues(typeof(room)))
                {
                    SetText(group, green);
                }
            }
            public void SetText(room rm, Color color)
            {
                List<IMyTextPanel> objs = list.Where(x => x.CustomName.Contains("[" + rm.ToString() + "]")).ToList();
                foreach (IMyTextPanel obj in objs)
                {
                    obj.SetValue("Content", (Int64)1);
                    obj.SetValueColor("FontColor", color);
                    obj.SetValueFloat("FontSize", 7.0f);
                    obj.SetValue("alignment", (Int64)2);
                    obj.WriteText(name_room[(int)rm].ToUpper(), false);
                }
            }
        }
        // Вентиляторы
        public class AirVent : BaseListTerminalBlock<IMyAirVent>
        {
            public AirVent(string name_obj) : base(name_obj)
            {

            }
            public VentStatus? getStatus(string tag)
            {
                IMyAirVent obj = list_obj.Where(x => x.CustomName.Contains(tag)).FirstOrDefault();
                return obj != null ? (VentStatus?)obj.Status : null;
            }
            public float? GetOxygenLevel(string tag)
            {
                IMyAirVent obj = list_obj.Where(x => x.CustomName.Contains(tag)).FirstOrDefault();
                return obj != null ? (float?)obj.GetOxygenLevel() : null;
            }
        }
        // Класс формирования подписей над дверями с учетом кислорода в помещении
        public class AirInfo
        {
            InfoTablo info_tablo;
            AirVent air_vant;
            public AirInfo(string name_obj, string tag)
            {
                info_tablo = new InfoTablo(name_obj, tag);
                info_tablo.InitPanel();
                air_vant = new AirVent(name_obj);
            }
            public void Logic(string argument, UpdateType updateSource)
            {
                switch (argument)
                {
                    //case "connected_on":
                    //    break;
                    default:
                        break;
                }

                if (updateSource == UpdateType.Update10)
                {
                    //test_lcd.WriteText("Старт" + "\n", false);
                    foreach (room group in Enum.GetValues(typeof(room)))
                    {
                        float? o2 = air_vant.GetOxygenLevel(group.ToString());
                        if (o2 != null)
                        {
                            if (o2 == 1)
                            {
                                info_tablo.SetText(group, green);
                            }
                            else if (o2 == 0)
                            {
                                info_tablo.SetText(group, red);
                            }
                            else
                            {
                                info_tablo.SetText(group, yellow);
                            }
                        }
                    }
                }

            }
        }
    }
}
