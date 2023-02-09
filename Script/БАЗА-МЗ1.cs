using Sandbox.Common.ObjectBuilders.Definitions;
using Sandbox.Game.Entities;
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
using VRage.ObjectBuilders;
using VRage.Utils;
using static Sandbox.Definitions.MyOxygenGeneratorDefinition;

// БАЗА-МЗ1
namespace БАЗА_МЗ1
{
    public sealed class Program : MyGridProgram
    {
        IMyTextPanel test_lcd, test_lcd1;

        string test_name = "БАЗА-МЗ1-Водородный генератор 1";
        IMyTerminalBlock test;

        string NameObj = "БАЗА-МЗ1";
        // Дверь левая выход в космос
        string NameDoorExt = "БАЗА-МЗ1-Раздвижная дверь external";
        string NameDoorInt = "БАЗА-МЗ1-Раздвижная дверь internal";
        sensor_option sensor_option_ext = new sensor_option()
        {
            name = "БАЗА-МЗ1-Сенсор вх.дверь external",
            lf = 1.0f,
            rg = 1.0f,
            bt = 3.0f,
            tp = 1.0f,
            bc = 0.0f,
            fr = 2.5f
        };
        sensor_option sensor_option_int = new sensor_option()
        {
            name = "БАЗА-МЗ1-Сенсор вх.дверь internal",
            lf = 1.0f,
            rg = 1.0f,
            bt = 3.0f,
            tp = 1.0f,
            bc = 0.0f,
            fr = 2.5f
        };
        DoorGateway door_gataway;
        InteriorLight light_room;       // Освещение
        GasTank gas_tank;               // Баки
        GasGenerator gas_generators;    // Генераторы газов


        int count_operator_room = 0; // Кол людей в помещениии

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
            static public string GetCountThrust(int count, int count_on)
            {
                return count + "|" + count_on;
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
            public enum MySubtypeName : int
            {
                none = 0,
                LargeBlockSmallAtmosphericThrust = 10,
                LargeBlockSmallAtmosphericThrustSciFi = 11,
                LargeBlockLargeAtmosphericThrust = 110,
                LargeBlockLargeAtmosphericThrustSciFi = 111,

                LargeBlockSmallHydrogenThrust = 20,
                LargeBlockSmallHydrogenThrustSciFi = 21,
                LargeBlockLargeHydrogenThrust = 120,
                LargeBlockLargeHydrogenThrustSciFi = 121,

                LargeBlockSmallThrust = 30,
                LargeBlockSmallThrustSciFi = 31,
                LargeBlockLargeThrust = 130,
                LargeBlockLargeThrustSciFi = 131,

                LargeHydrogenEngine = 40,
            }
            public List<T> list_obj = new List<T>();
            public int Count { get { return list_obj.Count(); } }
            public BaseListTerminalBlock(string name_obj)
            {
                _scr.GridTerminalSystem.GetBlocksOfType<T>(list_obj, r => ((IMyTerminalBlock)r).CustomName.Contains(name_obj));
                _scr.Echo(typeof(T).Name + "[" + name_obj + "]" + ((list_obj != null && list_obj.Count() > 0) ? ("Ок") : ("not found"))); ;
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

        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            _scr = this;

            test = GridTerminalSystem.GetBlockWithName(test_name) as IMyTerminalBlock;
            // тест LCD
            test_lcd = GridTerminalSystem.GetBlockWithName("БАЗА-МЗ1-test_lcd") as IMyTextPanel;
            Echo("test_lcd: " + ((test_lcd != null) ? ("Ок") : ("not found")));
            test_lcd1 = GridTerminalSystem.GetBlockWithName("БАЗА-МЗ1-test_lcd1") as IMyTextPanel;
            Echo("test_lcd: " + ((test_lcd != null) ? ("Ок") : ("not found")));
            door_gataway = new DoorGateway(sensor_option_ext, sensor_option_int, NameDoorExt, NameDoorInt);
            light_room = new InteriorLight(NameObj);    // Освещение
            light_room.Off();
            gas_tank = new GasTank(NameObj);            // БАКИ
            gas_tank.On();
            gas_generators = new GasGenerator(NameObj); // ГЕНЕРАТОРЫ

        }
        public void Save()
        {

        }
        public void Main(string argument, UpdateType updateSource)
        {
            //test_lcd.WriteText("old:" + old_parking.ToString() + " connector:"+connector.IsParkingEnabled.ToString() + "\n" , true);  
            //test_lcd.WriteText("clock="+ clock.ToString() +"updateSource-" + updateSource + "\n", false);            
            // Проверим логику
            int x = 0;
            // Шлюзовая дверь
            door_gataway.Logic(argument, updateSource, ref count_operator_room, ref x);
            switch (argument)
            {
                //case "connected_on":
                //    break;
                default:
                    break;
            }
            if (updateSource == UpdateType.Update10)
            {
                StringBuilder values = new StringBuilder();
                //DisplayBlockInfo(ref values, test);
                //test_lcd1.WriteText(values, false);                

                test_lcd.WriteText("IsInputDoor=" + door_gataway.IsInputDoor + "\n", false);
                test_lcd.WriteText("IsOutputDoor=" + door_gataway.IsOutputDoor + "\n", true);
                test_lcd.WriteText("count_operator_room=" + count_operator_room + "\n", true);
                test_lcd.WriteText(gas_generators.GetStatusOfText(), true);
                test_lcd.WriteText(gas_tank.GetStatusOfText(), true);
                // контроль освещения
                if (count_operator_room > 0)
                {
                    light_room.OnOfGroup("operator_room");
                }
                else
                {
                    light_room.OffOfGroup("operator_room");
                    count_operator_room = 0;
                }
            }
        }
        //------------------------------------------------------------
        public class sensor_option
        {
            public string name { get; set; }
            public float lf { get; set; }   //Left - Охват слева
            public float rg { get; set; }   //Right - Охват справа
            public float bt { get; set; }   //Bottom - Охват снизу
            public float tp { get; set; }   //Top - Охват сверху
            public float bc { get; set; }   //Back - Охват сзади
            public float fr { get; set; }   //Front - Охват спереди
        }
        public class Sensor
        {
            IMySensorBlock sensor;
            public bool PlayProximitySound { get { return sensor.PlayProximitySound; } set { sensor.PlayProximitySound = value; } }
            public bool IsActive { get { return sensor.IsActive; } }
            // Сенсор
            public Sensor(string name)
            {
                sensor = _scr.GridTerminalSystem.GetBlockWithName(name) as IMySensorBlock;
                _scr.Echo("sensor[" + name + "]: " + ((sensor != null) ? ("Ок") : ("not found")));

                SetExtend(0.1f, 0.1f, 0.1f, 0.1f, 0.1f, 0.1f);
                sensor.PlayProximitySound = true;       // звук
                SetDetect(true, false, false, false, false, false, false, true, false, false, false);
            }
            public Sensor(string name, float lf, float rg, float bt, float tp, float bc, float fr)
            {
                sensor = _scr.GridTerminalSystem.GetBlockWithName(name) as IMySensorBlock;
                _scr.Echo("sensor[" + name + "]: " + ((sensor != null) ? ("Ок") : ("not found")));

                SetExtend(lf, rg, bt, tp, bc, fr);
                SetDetect(true, false, false, false, false, false, false, true, false, false, false);
            }
            public Sensor(sensor_option setup)
            {
                sensor = _scr.GridTerminalSystem.GetBlockWithName(setup.name) as IMySensorBlock;
                _scr.Echo("sensor[" + setup.name + "]: " + ((sensor != null) ? ("Ок") : ("not found")));

                SetExtend(setup.lf, setup.rg, setup.bt, setup.tp, setup.bc, setup.fr);
                SetDetect(true, false, false, false, false, false, false, true, false, false, false);
            }

            public void SetExtend(float lf, float rg, float bt, float tp, float bc, float fr)
            {
                sensor.LeftExtend = lf;//Left - Охват слева
                sensor.RightExtend = rg;//Right - Охват справа
                sensor.BottomExtend = bt;//Bottom - Охват снизу
                sensor.TopExtend = tp;//Top - Охват сверху
                sensor.BackExtend = bc;//Back - Охват сзади
                sensor.FrontExtend = fr;//Front - Охват спереди
            }
            public void SetDetect(bool Players, bool FloatingObjects, bool SmallShips, bool LargeShips, bool Stations, bool Subgrids,
                bool Asteroids, bool Owner, bool Friendly, bool Neutral, bool Enemy)
            {
                sensor.DetectPlayers = Players;            // Играки
                sensor.DetectFloatingObjects = FloatingObjects;   // Обнаруживать плавающие объекты
                sensor.DetectSmallShips = SmallShips;        // Малые корабли
                sensor.DetectLargeShips = LargeShips;        // Большие корабли
                sensor.DetectStations = Stations;          // Большие станции
                sensor.DetectSubgrids = Subgrids;          // Подсетки
                sensor.DetectAsteroids = Asteroids;         // Астероиды планеты
                sensor.DetectOwner = Owner;              // Владельцы блоков
                sensor.DetectFriendly = Friendly;          // Дружественные игроки
                sensor.DetectNeutral = Neutral;           // Нитральные игроки
                sensor.DetectEnemy = Enemy;             // Враги
            }

        }
        public class Door
        {
            IMyDoor door;
            public DoorStatus Status { get { return door.Status; } }
            public Door(string name)
            {
                door = _scr.GridTerminalSystem.GetBlockWithName(name) as IMyDoor;
                _scr.Echo("door[" + name + "]: " + ((door != null) ? ("Ок") : ("not door")));
            }

            public void Open()
            {
                door.OpenDoor();
            }
            public void Close()
            {
                door.CloseDoor();
            }


        }
        // Класс Шлюзовая дверь
        public class DoorGateway
        {

            Sensor sn_door_external;
            Sensor sn_door_internal;
            Door door_external;
            Door door_internal;

            bool input_door = false;
            bool output_door = false;
            bool internal_active = false;    // датчик входа
            bool external_active = false;   // датчик выхода

            public bool IsActiveSensorInternal { get { return sn_door_internal.IsActive; } }
            public bool IsActiveSensorExternal { get { return sn_door_internal.IsActive; } }

            public bool IsInputDoor { get { return input_door; } set { input_door = value; } }  // Вошол 
            public bool IsOutputDoor { get { return output_door; } set { output_door = value; } } // Вышел 
            public DoorGateway(sensor_option sensor_option_ext, sensor_option sensor_option_int, string NameDoorExt, string NameDoorInt)
            {
                // Создадим объекты 
                sn_door_external = new Sensor(sensor_option_ext);
                sn_door_internal = new Sensor(sensor_option_int);
                door_external = new Door(NameDoorExt);
                door_internal = new Door(NameDoorInt);

            }
            public void Logic(string argument, UpdateType updateSource, ref int count_input, ref int count_output)
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
                    if (!sn_door_internal.IsActive && door_internal.Status == DoorStatus.Open)
                    {
                        // Игрок не найден возле внутр двери
                        door_internal.Close();
                    }
                    if (sn_door_internal.IsActive && door_internal.Status == DoorStatus.Closed && door_external.Status == DoorStatus.Closed)
                    {
                        // Игрокнайден возле внутр дверь закрыта и внешняя закрыта
                        door_internal.Open();
                    }
                    if (!sn_door_external.IsActive && door_external.Status == DoorStatus.Open)
                    {
                        // Игрок не найден возле внутр двери
                        door_external.Close();
                    }
                    if (sn_door_external.IsActive && door_external.Status == DoorStatus.Closed && door_internal.Status == DoorStatus.Closed)
                    {
                        // Игрокнайден возле внутр дверь закрыта и внешняя закрыта
                        door_external.Open();
                    }
                    // Логика направдения движения
                    if (internal_active && !external_active && sn_door_external.IsActive)
                    {
                        // Выход
                        //internal_active = false;
                        external_active = true;
                        input_door = false;
                        output_door = true;
                        count_input--;
                        count_output++;
                    }
                    if (external_active && !internal_active && sn_door_internal.IsActive)
                    {
                        // Вход
                        internal_active = true;
                        //external_active = false;
                        input_door = true;
                        output_door = false;
                        count_input++;
                        count_output--;
                    }
                    if (external_active && internal_active && !sn_door_external.IsActive && !sn_door_internal.IsActive)
                    {
                        // Вход
                        internal_active = false;
                        external_active = false;
                        input_door = false;
                        output_door = false;
                    }

                    if (!internal_active && !external_active)
                    {
                        // Выход
                        internal_active = sn_door_internal.IsActive;
                        external_active = sn_door_external.IsActive;
                    }
                }

            }
            public void ClearMoveDoor()
            {
                input_door = false;
                output_door = false;
                internal_active = false;    // датчик входа
                external_active = false;   // датчик выхода
            }

        }
        // Класс лампы
        public class InteriorLight : BaseListTerminalBlock<IMyInteriorLight>
        {
            public InteriorLight(string name_obj) : base(name_obj)
            {

            }
        }
        // Класс Баки
        public class GasTank : BaseListTerminalBlock<IMyGasTank>
        {
            public GasTank(string name_obj) : base(name_obj)
            {

            }
            public string GetStatusOfText()
            {
                string tdh2 = "";
                string tdo2 = "";
                double fr_h2 = 0;
                float cap_h2 = 0;
                int count_th2 = 0;
                double fr_o2 = 0;
                float cap_o2 = 0;
                int count_to2 = 0;
                foreach (IMyGasTank obj in list_obj)
                {
                    switch (obj.DefinitionDisplayNameText)
                    {
                        case "Водородный бак":
                            {
                                fr_h2 += obj.FilledRatio;
                                cap_h2 += obj.Capacity;
                                count_th2++;
                                tdh2 += "|  |-БАК:[" + (obj.Enabled ? "{+}" : "{-}") + (obj.Stockpile ? "{>}" : "{<}") + (obj.AutoRefillBottles ? "{A}" : "{ }") + "]" + PText.GetPersent(obj.FilledRatio) + PText.GetCapacityTanks(obj.FilledRatio, obj.Capacity) + "\n";
                                break;
                            }
                        case "Кислородный бак":
                            {
                                fr_o2 += obj.FilledRatio;
                                cap_o2 += obj.Capacity;
                                count_to2++;
                                tdo2 += "|  |-БАК:[" + (obj.Enabled ? "{+}" : "{-}") + (obj.Stockpile ? "{>}" : "{<}") + (obj.AutoRefillBottles ? "{A}" : "{ }") + "]" + PText.GetPersent(obj.FilledRatio) + PText.GetCapacityTanks(obj.FilledRatio, obj.Capacity) + "\n";
                                break;
                            }
                    }
                }
                string result = "";
                result += "|    H2:" + PText.GetCapacityTanks((fr_h2 / count_th2), cap_h2) + "\n";
                result += "|-+" + PText.GetScalePersent((fr_h2 / count_th2), 50) + "\n";
                result += tdh2;
                result += "|\n";
                result += "|    O2:" + PText.GetCapacityTanks((fr_o2 / count_to2), cap_o2) + "\n";
                result += "|-+" + PText.GetScalePersent((fr_o2 / count_to2), 50) + "\n";
                result += tdo2;
                result += "|\n";
                return result;
            }
        }
        // Класс генераторы
        public class GasGenerator : BaseListTerminalBlock<IMyGasGenerator>
        {
            public class valus_gas_generator
            {
                public int id_group = 0;
                public MySubtypeName subtype_name = MySubtypeName.none;
                public string definition_display_name_text = null;
                public int count = 0;                       // кол
                public int count_on = 0;                    // кол вкл
                public int count_auto_refill = 0;           // кол авто заполнения
                public int count_use_conveyor_system = 0;   // кол авто заполнения

                public float sum_to = 0;                    // перехват тяги тяга МН
                public float sum_to_percent = 0;            // процент от макс перехват тяги тяга %
                public float sum_max_thrust = 0;            // Макс тяга МН
                public float sum_max_eff_thrust = 0;        // Макс эфектив тяга МН
                public float sum_cur_thrust = 0;            // Текущая тяга МН
            }

            public List<valus_gas_generator> list_gas_generator = new List<valus_gas_generator>();

            public GasGenerator(string name_obj) : base(name_obj)
            {

            }

            public void GetValueGasGenerator()
            {
                list_gas_generator.Clear();

                _scr.test_lcd1.WriteText("Старт" + "\n", false);

                foreach (IMyGasGenerator obj in list_obj)
                {
                    valus_gas_generator val_obj = list_gas_generator.Where(o => o.subtype_name.ToString() == obj.BlockDefinition.SubtypeName).FirstOrDefault();
                    //_scr.test_lcd1.WriteText("location=" + getLocation(obj, is_control) + "\n", true);
                    if (val_obj == null)
                    {
                        _scr.test_lcd1.WriteText("val_obj=" + obj.BlockDefinition.SubtypeName.ToString() + "\n", true);
                        // Инвентарь
                        if (obj.HasInventory)
                        {
                            for (int i = 0; i < obj.InventoryCount; i++)
                            {
                                IMyInventory inv = obj.GetInventory(i);
                                if (inv != null)
                                {
                                    float curr_mass = ((float)inv.CurrentMass);
                                    float curr_vol = ((float)inv.CurrentVolume);
                                    float curr_max_vol = ((float)inv.MaxVolume);
                                    _scr.test_lcd1.WriteText("curr_mass=" + curr_mass + "curr_vol=" + curr_vol + "curr_max_vol=" + curr_max_vol + "\n", true);
                                }
                            }
                        }
                        //
                        MyResourceSinkComponent sink;
                        obj.Components.TryGet<MyResourceSinkComponent>(out sink);
                        if (sink != null)
                        {
                            var list = sink.AcceptedResources;
                            for (int j = 0; j < list.Count; ++j)
                            {
                                float currentInput = 0;
                                float maxRequiredInput = 0;
                                bool isPoweredBy = false;

                                currentInput = sink.CurrentInputByType(list[j]);
                                isPoweredBy = sink.IsPoweredByType(list[j]);
                                maxRequiredInput = sink.MaxRequiredInputByType(list[j]);
                                //            float available = sink.ResourceAvailableByType(list[j]); // Prohibited
                                _scr.test_lcd1.WriteText("Current=" + currentInput.ToString() + " Max=" + maxRequiredInput.ToString() + " PoweredBy=" + isPoweredBy.ToString() + "\n", true);
                            }
                        }
                        MyResourceSourceComponent source;
                        obj.Components.TryGet<MyResourceSourceComponent>(out source);

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
                                float maxoutput = source.DefinedOutputByType(list[j]);
                                float currentoutput = source.CurrentOutputByType(list[j]);
                                _scr.test_lcd1.WriteText(list[j].SubtypeId.ToString() + " Current=" + currentoutput + " Max=" + maxoutput + "\n", true);
                            }
                        }
                        val_obj = new valus_gas_generator()
                        {
                            id_group = 0,
                            definition_display_name_text = obj.DefinitionDisplayNameText,
                            subtype_name = (MySubtypeName)Enum.Parse(typeof(MySubtypeName), obj.BlockDefinition.SubtypeName.ToString()),
                            count = 1,
                            count_on = obj.Enabled ? 1 : 0,

                        };
                        list_gas_generator.Add(val_obj);
                    }
                    else
                    {
                        val_obj.count++;
                        if (obj.Enabled) val_obj.count_on++;
                    }
                }
            }

            public string GetStatusOfText()
            {
                string result = "ГЕН.H2/O2: ";
                foreach (IMyGasGenerator obj in list_obj)
                {
                    //result += "MaxCapacity " + obj.GetProperty("MaxCapacity").TypeName + "\n";
                    //result += obj.BlockDefinition.ToString()+ "\n";
                    //((MyObjectBuilder_OxygenGeneratorDefinition)obj).
                    //List<MyGasGeneratorResourceInfo> list = obj.BlockDefinition.;
                    //List<ITerminalProperty> list = new List<ITerminalProperty>();
                    //obj.GetProperties(list);

                    //MyValueFormatter.AppendWorkInBestUnit(ResourceSink.MaxRequiredInputByType(MyResourceDistributorComponent.ElectricityId), DetailedInfo)

                    //foreach (ITerminalProperty x in list)
                    //{
                    //    result += " - " + (x.Id) + "\n";
                    //}
                    //SerializableDefinitionId dd = obj.BlockDefinition;


                    //MyResourceSinkComponent sink;
                    //obj.Components.TryGet<MyResourceSinkComponent>(out sink);
                    //if (sink != null)
                    //{
                    //    VRage.Collections.ListReader<VRage.Game.MyDefinitionId> list = sink.AcceptedResources;
                    //    for (int j = 0; j < list.Count; ++j)
                    //    {
                    //        result += ("\n " + list[j].SubtypeId.ToString() + " (" + list[j].SubtypeName + ")");
                    //        //Echo(list[j].SubtypeId.ToString() + " (" + list[j].SubtypeName + ")");

                    //        float currentInput = 0;
                    //        float maxRequiredInput = 0;
                    //        bool isPoweredBy = false;

                    //        currentInput = sink.CurrentInputByType(list[j]);
                    //        isPoweredBy = sink.IsPoweredByType(list[j]);
                    //        maxRequiredInput = sink.MaxRequiredInputByType(list[j]);
                    //        //            float available = sink.ResourceAvailableByType(list[j]); // Prohibited


                    //        result += ("\n Current=" + currentInput.ToString() + " Max=" + maxRequiredInput.ToString() + " PoweredBy=" + isPoweredBy.ToString()
                    //            );

                    //    }
                    //}


                    result += "[" + (obj.Enabled ? "+" : "-") + "]";
                }
                result += "\n";
                return result;
            }
        }
    }
}
