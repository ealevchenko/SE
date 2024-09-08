﻿using Sandbox.Game.GameSystems;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace PMP
{
    public sealed class Program : MyGridProgram
    {



        /*//////////////////////////
         * Thank you for using:
         * [PAM] - Path Auto Miner
         * ————————————
         * Author:  Keks
         * Last update: 2019-12-20
         * ————————————
         * Guide: https://steamcommunity.com/sharedfiles/filedetails/?id=1553126390
         * Script: https://steamcommunity.com/sharedfiles/filedetails/?id=1507646929
         * Youtube: https://youtu.be/ne_i5U2Y8Fk
         * ————————————
         * Please report bugs here:
         * https://steamcommunity.com/workshop/filedetails/discussion/1507646929/2727382174640941895/
         * ————————————
         * I hope you enjoy this script and don't forget
         * to leave a comment on the steam workshop
         *//////////////////////////

        const string VERSION = "1.3.1";
        const string DATAREV = "14";

        const String pamTag = "[PAM]";
        const String controllerTag = "[PAM-Controller]";
        //Tag for LCD's of cockpits and other blocks: [PAM:<lcdIndex>] e.g: [PAM:1]

        const int gyroSpeedSmall = 15; //[RPM] small ship
        const int gyroSpeedLarge = 5; //[RPM] large ship
        const int generalSpeedLimit = 100; //[m/s] 0 = no limit (if set to 0 ignore unreachable code warning)
        const float dockingSpeed = 0.5f; //[m/s]

        //multiplied with ship size
        const float dockDist = 0.6f; //position in front of the home connector
        const float followPathDock = 2f; //stop following path, fly direct to target
        const float followPathJob = 1f; //same as dock
        const float useDockDirectionDist = 1f; //Override waypoint direction, use docking dir
        const float useJobDirectionDist = 0f; //same as dock

        //other distances
        const float wpReachedDist = 2f;//[m]
        const float drillRadius = 1.4f;//[m]

        //grinding
        const float sensorRange = 2f;//fly slow when blocks found in this range
        const float fastSpeed = 10f;//speed when no blocks are detected

        //minimum acceleration in space before ship becomes too heavy
        const float minAccelerationSmall = 0.2f;//[m/s²] small ship
        const float minAccelerationLarge = 0.1f;//[m/s²] large ship

        //stone ejection
        const float minEjection = 25f;//[%] Min amount of ejected cargo to continue job

        //LCD
        const bool setLCDFontAndSize = true;

        //Check if blocks are connected with conveyors
        const bool checkConveyorSystem = false;//temporarily disabled because of a SE bug


        public String GetElementCode(String itemName)
        {
            //Here you can define custom element codes for the PAM-Controller
            //You can extend this when you are using mods which adds new materials
            //This is not necessary for any function of PAM, it is just a little detail on the controller screen
            //It only needs to be changed in the controller pb
            switch (itemName)
            {
                case "IRON": return "Fe";
                case "NICKEL": return "Ni";
                case "COBALT": return "Co";
                case "MAGNESIUM": return "Mg";
                case "SILICON": return "Si";
                case "SILVER": return "Ag";
                case "GOLD": return "Au";
                case "PLATINUM": return "Pt";
                case "URANIUM": return "U";

                //add new entries here

                //example:
                //New material: ExampleOre
                //Element code: Ex

                //case "EXAMPLEORE": return "Ex";

                default: return ""; //don't change this!
            }
        }
        Program()
        {
            χ = GridTerminalSystem; Runtime.UpdateFrequency = UpdateFrequency.Update10; if (ї(Me, controllerTag, true)) ƽ = В.А; Љ(ϸ.Ϸ)
        ; if (ƽ != В.А)
            {
                ώ = "Welcome to [PAM]!"; ɚ ϊ = ɖ(); if (ƽ == В.ǻ)
                {
                    List<IMyShipDrill> ω = new List<IMyShipDrill>(); List<IMyShipGrinder> ψ =
        new List<IMyShipGrinder>(); χ.GetBlocksOfType(ω, q => q.CubeGrid == Me.CubeGrid); χ.GetBlocksOfType(ψ, q => q.CubeGrid == Me.CubeGrid);
                    if (ω.Count > 0) { ƽ = В.Б; ώ = "Miner mode enabled!"; } else if (ψ.Count > 0) { ƽ = В.ψ; ώ = "Grinder mode enabled!"; } else { ƽ = В.Ͼ; Ͻ(Ѓ.Ͼ); }
                }
                if (ϊ
                    == ɚ.ə) ν = false; if (ϊ == ɚ.ɘ) ώ = "Data restore failed!"; if (ϊ == ɚ.Ə) ώ = "New version, wipe data";
            }
        }
        IMyGridTerminalSystem χ; Vector3 φ =
                    new Vector3(); Vector3 ɉ = new Vector3(); Vector3 υ = new Vector3(); Vector3 τ = new Vector3(); Vector3 σ = new Vector3(); DateTime ς =
                    new DateTime(); bool ρ = true; int π = 0; bool ο = false; bool ξ = false; bool ν = true; bool ƿ = false; bool μ = false; bool λ = false; float κ = 0;
        float ϋ = 0; int ι = 0; int Ə = 0; int Ϊ = 0; int Ω = 0; float Ψ = 0; float Χ = 0; double Φ = 0; float Υ = 0; List<int> Τ = new List<int>(); List<int> Σ = new
        List<int>(); void Main(string Ƹ, UpdateType Ρ)
        {
            try
            {
                if (ȥ != null) { Ҕ(); Ȥ(); return; }
                ξ = (Ρ & UpdateType.Update10) != 0; if (ξ) π++; ο = π >= 10;
                if (ο) π = 0; if (ξ) { ϒ++; if (ϒ > 4) ϒ = 0; Ϊ = Math.Max(0, 10 - (DateTime.Now - ς).Seconds); }
                if (Ƹ != "") ȿ = Ƹ; if (ƽ != В.А) Λ(Ƹ); else ǌ(Ƹ); Њ = false; try
                {
                    int Ο = Runtime.CurrentInstructionCount; float A = µ(Ο, Runtime.MaxInstructionCount); if (A > 0.90) ώ = "Max. instructions >90%"; if (A
                > Ψ) Ψ = A; if (Ν)
                    {
                        Τ.Add(Ο); while (Τ.Count > 10) Τ.RemoveAt(0); Χ = 0; for (int ã = 0; ã < Τ.Count; ã++) { Χ += Τ[ã]; }
                        Χ = µ(µ(Χ, Τ.Count), Runtime.
                MaxInstructionCount); double Ξ = Runtime.LastRunTimeMs; if (λ && Ξ > Φ) Φ = Ξ; Σ.Add((int)(Ξ * 1000f)); while (Σ.Count > 10) Σ.RemoveAt(0); Υ = 0; for (int ã = 0; ã < Σ.
                Count; ã++) { Υ += Σ[ã]; }
                        Υ = µ(Υ, Σ.Count) / 1000f;
                    }
                }
                catch { Ψ = 0; }
            }
            catch (Exception e) { ȥ = e; }
        }
        bool Ν = false; bool Μ = false; void Λ(string Ƹ)
        {
            bool Π = false; String Κ = ""; if (ɘ <= 1 && !Ⱦ(Ƹ)) η(Ƹ); if (ɬ != null && ɬ.HasPendingMessage)
            {
                MyIGCMessage ǉ = ɬ.AcceptMessage(); String ž = (
            string)ǉ.Data; String ǋ = ""; if (ɘ <= 1 && Ȼ(ref ž, out ǋ, out Κ) && ǋ == ɟ) { η(ž); Π = true; }
            }
            bool θ = λ && Ҡ == ҍ.Д && !μ && !Π && ι == 0 && !ˍ; if (ο && Ҡ != ҍ.Д) Π
            = true; if ((ξ && !θ) || (ο && θ))
            {
                if (ι == 0 && (Ϊ <= 0 || ρ)) { ƿ = ɘ > 0; ɘ = 0; ι = 1; ô(); Ѹ(); ɪ(); ó("Scan 1"); }
                else if (ι == 1)
                {
                    ι = 2; ô(); Ҁ(); ó("Scan 2"
            );
                }
                else if (ι == 2)
                {
                    ι = 0; ô(); Ѻ(); ó("Scan 3"); ς = DateTime.Now; if (ɘ <= 1 && ν) φ = Ȓ(Э, Э.CenterOfMass); ν = false; if (ρ) { ρ = false; Ҕ(); }
                    if (ƿ
            && ɘ == 0) ώ = "Setup complete";
                }
                else
                {
                    if (Ӄ == ӌ.Ӊ && ƽ != В.Ͼ) { ô(); ѓ(); ó("Inv balance"); }
                    ô(); switch (Ə)
                    {
                        case 0: Ħ(); break;
                        case 1:
                            ħ();
                            break;
                        case 2: İ(); break;
                        case 3: ķ(); break;
                        case 4: Ļ(); break;
                        case 5: ĳ(); break;
                        case 6: D(Э); break;
                    }
                    ó("Update: " + Ə); Ə++; if (Ə > 6)
                    {
                        Ə = 0;
                        λ = true; if (ɛ != ӌ.Ӌ)
                        {
                            switch (ɛ)
                            {
                                case ӌ.Ӈ: қ(); break;
                                case ӌ.ӆ: қ(); break;
                                case ӌ.Ӊ: қ(); break;
                                case ӌ.ғ: ҏ(); break;
                                case ӌ.ӈ:
                                    Ґ();
                                    break;
                            }
                            ɛ = ӌ.Ӌ;
                        }
                    }
                }
                if (!ρ)
                {
                    if (!Ƃ(Э, true)) { Э = null; ν = true; ɘ = 2; }
                    if (ɘ >= 2 && Ҡ != ҍ.Д) Ҕ(); if (ɘ <= 1)
                    {
                        ϋ = Э.CalculateShipMass().PhysicalMass; κ =
                                    (float)Э.GetShipSpeed(); ɉ = Ȕ(Э, φ); υ = Э.WorldMatrix.Forward; τ = Э.WorldMatrix.Left; σ = Э.WorldMatrix.Down; ˇ(); if (Ҡ != ҍ.Д)
                        {
                            μ = false
                                    ; ű(false); È(false); String O = ϕ(Ҡ) + " " + (int)Ҡ; ô(); ӗ(); З(false); ó(O); ô(); ą(); ó("Thruster"); ô(); Ĕ(); ó("Gyroscope");
                        }
                        else
                        {
                            if (μ
                                    )
                            {
                                if (ù()) { ë(σ, υ, τ, 0.25f, true); Ĕ(); ώ = "Aligning to planet: " + Math.Round(ė - 0.25f, 2) + "°"; if (ě) α(true, true); }
                                else α(true, true)
                                    ;
                            }
                        }
                        Μ = false;
                    }
                }
            }
            ô(); ȟ(); ó("Print"); if (Π || Ω <= 0) { ô(); ȸ(Κ); ó("Broadcast"); Ω = 4; } else if (ο) Ω--;
        }
        void η(string Ƹ)
        {
            if (Ƹ == "") return
                                    ; var ª = Ƹ.ToUpper().Split(' '); ª.DefaultIfEmpty(""); var Ƽ = ª.ElementAtOrDefault(0); var Ʒ = ª.ElementAtOrDefault(1); var ζ = ª.
                                    ElementAtOrDefault(2); var ε = ª.ElementAtOrDefault(3); String ǆ = "Invalid argument: " + Ƹ; bool δ = false; switch (Ƽ)
            {
                case "UP": this.Ј(false); break;
                case "DOWN": this.Ї(false); break;
                case "UPLOOP": this.Ј(true); break;
                case "DOWNLOOP": this.Ї(true); break;
                case "APPLY":
                    this.ϣ(true);
                    break;
                case "MRES": Ѕ = 0; break;
                case "STOP": this.Ҕ(); break;
                case "PATHHOME": { this.Ҕ(); this.ʹ(); } break;
                case "PATH":
                    {
                        this.Ҕ(); this.ʹ(); Β
                    .Ͷ = true;
                    }
                    break;
                case "START": { this.Ҕ(); Ұ(); } break;
                case "ALIGN": { α(!μ, false); } break;
                case "CONT": { this.Ҕ(); this.қ(); } break;
                case
                    "JOBPOS":
                    { this.Ҕ(); this.Ґ(); }
                    break;
                case "HOMEPOS": { this.Ҕ(); this.ҏ(); } break;
                case "FULL": { Ɔ = true; } break;
                case "RESET":
                    { ɜ = true; ɘ = 2; }
                    break;
                default: δ = true; break;
            }
            if (ƽ != В.Ͼ)
            {
                switch (Ƽ)
                {
                    case "SHUTTLE": { β(); } break;
                    case "CFGS": { if (!ί(Ʒ, ζ, ε)) ώ = ǆ; } break;
                    case "CFGB":
                        {
                            if
                    (!Ϲ(Ʒ, ζ)) ώ = ǆ;
                        }
                        break;
                    case "CFGL": { if (!ʈ(ref ɹ, true, ʉ.Ă, Ʒ, "") || !Ϻ(ζ)) ώ = ǆ; } break;
                    case "CFGE":
                        {
                            if (!ʈ(ref ɷ, true, ʉ.ʏ, Ʒ, "IG") || !ʈ
                    (ref ɸ, true, ʉ.ʐ, ζ, "IG") || !ʈ(ref ɶ, true, ʉ.ʎ, ε, "IG")) ώ = ǆ;
                        }
                        break;
                    case "CFGA": { if (!ʈ(ref ɵ, false, ʉ.ª, Ʒ, "")) ώ = ǆ; } break;
                    case
                    "CFGW":
                        { if (!ʈ(ref ɴ, false, ʉ.ʓ, Ʒ, "") || !ʈ(ref ɳ, false, ʉ.ʓ, ζ, "")) ώ = ǆ; }
                        break;
                    case "NEXT": { Ά(false); } break;
                    case "PREV":
                        { Ά(true); }
                        break;
                    default: if (δ) ώ = ǆ; break;
                }
            }
            else { switch (Ƽ) { case "UNDOCK": { Μ = true; } break; default: if (δ) ώ = ǆ; break; } }
        }
        String γ()
        {
            String O =
                        "\n\n" + "Run-arguments: (Type without:[ ])\n" + "———————————————\n" + "[UP] Menu navigation up\n" + "[DOWN] Menu navigation down\n" +
                        "[APPLY] Apply menu point\n\n" + "[UPLOOP] \"UP\" + looping\n" + "[DOWNLOOP] \"DOWN\" + looping\n" + "[PATHHOME] Record path, set home\n" +
                        "[PATH] Record path, use old home\n" + "[START] Start job\n" + "[STOP] Stop every process\n" + "[CONT] Continue last job\n" + "[JOBPOS] Move to job position\n" +
                        "[HOMEPOS] Move to home position\n\n" + "[FULL] Simulate ship is full\n" + "[ALIGN] Align the ship to planet\n" + "[RESET] Reset all data\n"; if (ƽ != В.Ͼ) O +=
                        "[SHUTTLE] Enable shuttle mode\n" + "[NEXT] Next hole\n" + "[PREV] Previous hole\n\n" + "[CFGS width height depth]*\n" + "[CFGB done damage]*\n" +
                        "[CFGL maxload weightLimit]*\n" + "[CFGE minUr minBat minHyd]*\n" + "[CFGW forward backward]*\n" + "[CFGA acceleration]*\n" + "———————————————\n" +
                        "*[CFGS] = Config Size:\n" + " e.g.: \"CFGS 5 3 20\"\n\n" + "*[CFGB] = Config Behaviour:\n" + " When done: [HOME,STOP]\n" +
                        " On Damage: [HOME,JOB,STOP,IG]\n" + " e.g.: \"CFGB HOME IG\"\n\n" + "*[CFGL] = Config max load:\n" + " maxload: [10..95]\n" + " weight limit: [On/Off]\n" +
                        " e.g.: \"CFGL 70 on\"\n\n" + "*[CFGE] = Config energy:\n" + " minUr (Uranium): [1..25, IG]\n" + " minBat (Battery): [5..30, IG]\n" +
                        " minHyd (Hydrogen): [10..90, IG]\n" + " e.g.: \"CFGE 20 10 IG\"\n\n" + "*[CFGW] = Config work speed:\n" + " fwd: [0.5..10]\n" + " bwd: [0.5..10]\n" +
                        " e.g.: \"CFGW 1.5 2\"\n\n" + "*[CFGA] = Config acceleration:\n" + " acceleration: [10..100]\n" + " e.g.: \"CFGA 80\"\n";
            else O +=
                        "[UNDOCK] Leave current connector\n\n"; return O;
        }
        void β() { Ҕ(); ƽ = В.Ͼ; Ͻ(Ѓ.Ͼ); Ǟ.Ͷ = false; Β.Ͷ = false; Ь = null; Ш.Clear(); Ӄ = ӌ.Ӌ; }
        void α(bool ά, bool ΰ)
        {
            if (!ά) ώ =
                        "Aligning canceled"; if (ΰ) ώ = "Aligning done"; if (ΰ || !ά) { μ = false; ì(); á(false, 0, 0, 0, 0); return; }
            if (ù()) μ = true;
        }
        bool ί(String ή, String έ, String Ϋ)
        {
            bool ά = Ӄ == ӌ.Ӊ; int Y, X, ț; if (int.TryParse(ή, out Y) && int.TryParse(έ, out X) && int.TryParse(Ϋ, out ț))
            {
                this.Ҕ(); ʃ = Y; ʂ = X; ʁ = ț; ʸ(
        false); Қ(false, false); if (ά) қ(); return true;
            }
            return false;
        }
        bool Ϻ(String ʓ)
        {
            if (ʓ == "ON") { ɿ = true; return true; }
            if (ʓ == "OFF")
            {
                ɿ =
        false; return true;
            }
            return false;
        }
        bool Ϲ(String ț, String Ʀ)
        {
            bool J = true; if (ț == "HOME") ʅ = true;
            else if (ț == "STOP") ʅ = false;
            else J =
        false; if (Ʀ == "HOME") ʆ = ʯ.ʮ; else if (Ʀ == "STOP") ʆ = ʯ.ʬ; else if (Ʀ == "JOB") ʆ = ʯ.ʭ; else if (Ʀ == "IG") ʆ = ʯ.ʫ; else J = false; return J;
        }
        public
        enum ϸ
        { Ϸ, ϵ, ϴ, ϳ, ϲ, ϱ, ϰ, ϯ, Ϯ, ϭ, Ϭ, Ƿ, ž, ϻ, ϼ, Ў, Џ, Ѝ, Ќ }
        int[] Ћ = new int[Enum.GetValues(ϸ.ž.GetType()).Length]; bool Њ = false; void Љ(ϸ ʻ)
        {
            Ћ
        [(int)Є] = Ѕ; Ѕ = Ћ[(int)ʻ]; if (ʻ == ϸ.Ϭ) Ѕ = 0; Є = ʻ; if (ƽ != В.А) Ş(Є == ϸ.ϴ, false, 0, 0); Њ = true;
        }
        void Ј(bool ʹ)
        {
            if (Ѕ > 0) Ѕ--; else if (ʹ) Ѕ = І - 1;
        }
        void Ї(bool ʹ) { if (Ѕ < І - 1) Ѕ++; else if (ʹ) Ѕ = 0; }
        int І = 0; int Ѕ = 0; ϸ Є = ϸ.Ϸ; public enum Ѓ { Ђ, Ё, Ѐ, Ͽ, Ͼ }
        String Ͻ(Ѓ ƿ)
        {
            switch (ƿ)
            {
                case
        Ѓ.Ђ:
                    ώ = "Job is running"; break;
                case Ѓ.Ё: ώ = "Connector not ready!"; break;
                case Ѓ.Ѐ: ώ = "Ship modified, path outdated!"; break;
                case Ѓ.Ͽ: ώ = "Interrupted by player!"; break;
                case Ѓ.Ͼ: ώ = "Shuttle mode enabled!"; break;
            }
            return "";
        }
        String ϝ(ʲ ϫ)
        {
            switch (ϫ)
            {
                case ʲ
                .ʱ:
                    return "Top-Left";
                case ʲ.ʰ: return "Center";
                default: return "";
            }
        }
        String ϝ(ʶ Ϫ)
        {
            switch (Ϫ)
            {
                case ʶ.ʴ:
                    return "Auto" + (ƽ == В.Б ?
                " (Ore)" : "");
                case ʶ.ʳ: return "Auto (+Stone)";
                case ʶ.ʵ: return "Default";
                default: return "";
            }
        }
        String Ϝ(ӌ ϛ)
        {
            switch (ϛ)
            {
                case ӌ.Ӌ:
                    return
                "No job";
                case ӌ.ӊ: return "Job paused";
                case ӌ.Ӊ: return "Job active";
                case ӌ.Ӈ: return "Job active";
                case ӌ.ӆ: return "Job active";
                case ӌ.
                ΰ:
                    return "Job done";
                case ӌ.Ғ: return "Job changed";
                case ӌ.ғ: return "Move home";
                case ӌ.ӈ: return "Move to job";
            }
            return "";
        }
        String
                Ϛ(ʯ ϙ)
        {
            switch (ϙ)
            {
                case ʯ.ʮ: return "Return home";
                case ʯ.ʭ: return "Fly to job pos";
                case ʯ.ʬ: return "Stop";
                case ʯ.ʫ:
                    return
                "Ignore";
            }
            return "";
        }
        String Ϙ(ʪ ș)
        {
            switch (ș)
            {
                case ʪ.ɗ: return "Off";
                case ʪ.ʧ: return "Drop pos (Stone) ";
                case ʪ.ʦ:
                    return
                "Drop pos (Sto.+Ice)";
                case ʪ.ʩ: return "Cur. pos (Stone)";
                case ʪ.ʨ: return "Cur. pos (Sto.+Ice)";
                case ʪ.ʥ: return "In motion (Stone)";
                case ʪ.ʺ:
                    return "In motion (Sto.+Ice)";
            }
            return "";
        }
        String ϗ(И ϖ)
        {
            switch (ϖ)
            {
                case И.ɗ: return "No batteries";
                case И.Ŧ: return "Charging";
                case И
                    .Г:
                    return "Discharging";
            }
            return "";
        }
        String ϕ(ҍ ϛ)
        {
            String O = ƽ == В.Ͼ ? "target" : "job"; switch (ϛ)
            {
                case ҍ.Д: return "Idle";
                case ҍ.ʟ:
                    return "Flying to XY position";
                case ҍ.Ҍ: return ƽ == В.ψ ? "Grinding" : "Mining";
                case ҍ.ҝ: return "Returning";
                case ҍ.ң:
                    return
                    "Flying to drop pos";
                case ҍ.ұ: return "Returning to dock";
                case ҍ.ү: return "Flying to dock area";
                case ҍ.Ү: return "Flying to job area";
                case ҍ.Ҧ:
                    return "Flying to path";
                case ҍ.ҭ: return "Flying to job position";
                case ҍ.Ҭ: return "Approaching dock";
                case ҍ.ґ: return "Docking";
                case
                    ҍ.ҫ:
                    return "Aligning";
                case ҍ.Ҫ: return "Aligning";
                case ҍ.ҩ: return "Retry docking";
                case ҍ.Ҩ: return "Unloading";
                case ҍ.ҡ:
                    return
                    Ɗ;
                case ҍ.ҧ: return "Undocking";
                case ҍ.Ŧ: return "Charging batteries";
                case ҍ.Ĺ: return "Waiting for uranium";
                case ҍ.ļ:
                    return
                    "Filling up hydrogen";
                case ҍ.ҥ: return "Waiting for ejection";
                case ҍ.Ҥ: return "Waiting for ejection";
                case ҍ.Ң: return "Flying to drop pos";
            }
            return
                    "";
        }
        String ϓ(ſ Ų)
        {
            switch (Ų)
            {
                case ſ.ž: return "On \"Undock\" command";
                case ſ.Ž: return "On player entered cockpit";
                case ſ.Ż:
                    return "On ship is full";
                case ſ.ź: return "On ship is empty";
                case ſ.ż: return "On time delay";
                case ſ.Ÿ:
                    return
                    "On batteries empty(<25%)";
                case ſ.ŷ: return "On batteries empty(=0%)";
                case ſ.Ź: return "On batteries full";
                case ſ.ŵ: return "On hydrogen empty(<25%)";
                case ſ.Ŵ: return "On hydrogen empty(=0%)";
                case ſ.Ŷ: return "On hydrogen full";
            }
            return "";
        }
        int ϒ = 0; int ϑ = 0; int ϐ = 0; int Ϗ = 0; String
                ώ = ""; bool ύ(ref String J, int ϔ, int ό, bool ƛ, String ū)
        {
            І += 1; if (ϔ == ό) ū = ">" + ū + (ϒ >= 2 ? " ." : ""); else ū = " " + ū; J += ū + "\n"; return ϔ
                == ό && ƛ;
        }
        int ϩ = 0; int Ϩ = 0; int ϧ = 0; int Ϧ = 0; int ϥ = 0; int Ϥ = 0; String ϣ(bool ƛ)
        {
            int A = 0; int ƴ = Ѕ; І = 0; String Ʃ = "———————————————\n";
            String ƶ = "--------------------------------------------\n"; String Ɨ = ""; Ɨ += Ϝ(Ӄ) + " | " + (Β.Ͷ ? "Ready to dock" : "No dock") + "\n"; Ɨ += Ʃ;
            double Ϣ = Math.Max(Math.Round(this.Ԍ), 0); if (Є == ϸ.Ϸ)
            {
                bool O = ƽ == В.Ͼ; if (ύ(ref Ɨ, ƴ, A++, ƛ, " Record path & set home")) ʹ(); if (ƽ == В.Б)
                    if (ύ(ref Ɨ, ƴ, A++, ƛ, " Setup mining job")) Љ(ϸ.ϴ); if (ƽ == В.ψ) if (ύ(ref Ɨ, ƴ, A++, ƛ, " Setup grinding job")) Љ(ϸ.ϴ); if (ƽ == В.Ͼ) if (ύ(
                    ref Ɨ, ƴ, A++, ƛ, " Setup shuttle job")) Љ(ϸ.Ѝ); if (ύ(ref Ɨ, ƴ, A++, ƛ, " Continue job")) қ(); if (ύ(ref Ɨ, ƴ, A++, ƛ,
                    " Fly to home position")) ҏ(); if (ύ(ref Ɨ, ƴ, A++, ƛ, " Fly to job position")) Ґ(); if (ύ(ref Ɨ, ƴ, A++, ƛ, " Behavior settings")) if (O) Љ(ϸ.Џ); else Љ(ϸ.ϱ); if
                    (ύ(ref Ɨ, ƴ, A++, ƛ, " Info")) Љ(ϸ.ϯ); if (ƽ != В.Ͼ) if (ύ(ref Ɨ, ƴ, A++, ƛ, " Help")) Љ(ϸ.ϭ);
            }
            else if (Є == ϸ.ϴ)
            {
                double ϡ = Math.Round(ʃ * Й, 1)
                    ; double Ϡ = Math.Round(ʂ * Я, 1); String Ι = ""; if (ύ(ref Ι, ƴ, A++, ƛ, " Start new job!")) Ұ(); if (ύ(ref Ι, ƴ, A++, ƛ,
                    " Change current job")) { Қ(false, false); Љ(ϸ.Ϸ); }
                if (ύ(ref Ι, ƴ, A++, ƛ, " Width + (Width: " + ʃ + " = " + ϡ + "m)")) { ő(ref ʃ, 5, 20, 1); ʸ(true); }
                if (ύ(ref Ι, ƴ,
                    A++, ƛ, " Width -")) { ő(ref ʃ, -5, 20, -1); ʸ(true); }
                if (ύ(ref Ι, ƴ, A++, ƛ, " Height + (Height: " + ʂ + " = " + Ϡ + "m)"))
                {
                    ő(ref ʂ, 5, 20, 1); ʸ
                    (true);
                }
                if (ύ(ref Ι, ƴ, A++, ƛ, " Height -")) { ő(ref ʂ, -5, 20, -1); ʸ(true); }
                if (ύ(ref Ι, ƴ, A++, ƛ, " Depth + (" + (ʀ == ʶ.ʵ ? "Depth" : "Min"
                    ) + ": " + ʁ + "m)")) { ő(ref ʁ, 5, 50, 2); ʸ(true); }
                if (ύ(ref Ι, ƴ, A++, ƛ, " Depth -")) { ő(ref ʁ, -5, 50, -2); ʸ(true); }
                if (ύ(ref Ι, ƴ, A++, ƛ,
                    " Depth mode: " + ϝ(ʀ))) { ʀ = ͼ(ʀ); }
                if (ύ(ref Ι, ƴ, A++, ƛ, " Start pos: " + ϝ(ʄ))) { ʄ = ͼ(ʄ); }
                if (ƽ == В.ψ && ʀ == ʶ.ʳ) ʀ = ͼ(ʀ); Ɨ += ǧ(8, Ι, ƴ, ref ϥ);
            }
            else if (Є ==
                    ϸ.Ѝ)
            {
                float[] ʜ = new float[] { 0, 3, 10, 30, 60, 300, 600, 1200, 1800 }; if (ύ(ref Ɨ, ƴ, A++, ƛ, " Next")) { Љ(ϸ.Ќ); }
                if (ύ(ref Ɨ, ƴ, A++, ƛ, " Back"
                    )) { Љ(ϸ.Ϸ); }
                Ɨ += " Leave connector 1:\n"; if (ύ(ref Ɨ, ƴ, A++, ƛ, " - " + ϓ(ʔ.Ų))) ʔ.Ų = ͼ(ʔ.Ų); if (!ʔ.Ǝ()) Ɨ += "\n";
                else if (ύ(ref Ɨ, ƴ, A++
                    , ƛ, " - Delay: " + Ǖ((int)ʔ.Ƅ))) ʔ.Ƅ = ʞ(ʔ.Ƅ, ʜ); Ɨ += " Leave connector 2:\n"; if (ύ(ref Ɨ, ƴ, A++, ƛ, " - " + ϓ(ʒ.Ų))) ʒ.Ų = ͼ(ʒ.Ų); if (!ʒ.Ǝ(
                    )) Ɨ += "\n";
                else if (ύ(ref Ɨ, ƴ, A++, ƛ, " - Delay: " + Ǖ((int)ʒ.Ƅ))) ʒ.Ƅ = ʞ(ʒ.Ƅ, ʜ);
            }
            else if (Є == ϸ.Ќ)
            {
                if (ύ(ref Ɨ, ƴ, A++, ƛ,
                    " Start job!")) Ұ(); if (ύ(ref Ɨ, ƴ, A++, ƛ, " Back")) { Љ(ϸ.Ѝ); }
                Ɨ += " Timer: \"Docking connector 1\":\n"; if (ύ(ref Ɨ, ƴ, A++, ƛ, " = " + (ʔ.Ɣ != "" ? ʔ.Ɣ
                    : "-"))) ʔ.Ɣ = ʙ(ref ϩ); Ɨ += " Timer: \"Leaving connector 1\":\n"; if (ύ(ref Ɨ, ƴ, A++, ƛ, " = " + (ʔ.ƕ != "" ? ʔ.ƕ : "-"))) ʔ.ƕ = ʙ(ref ϧ); Ɨ +=
                    " Timer: \"Docking connector 2\":\n"; if (ύ(ref Ɨ, ƴ, A++, ƛ, " = " + (ʒ.Ɣ != "" ? ʒ.Ɣ : "-"))) ʒ.Ɣ = ʙ(ref Ϩ); Ɨ += " Timer: \"Leaving connector 2\":\n"; if (ύ(ref Ɨ, ƴ, A++, ƛ,
                    " = " + (ʒ.ƕ != "" ? ʒ.ƕ : "-"))) ʒ.ƕ = ʙ(ref Ϧ);
            }
            else if (Є == ϸ.ϳ)
            {
                String ϟ = ɹ + " %"; if (ο) ϑ++; if (ϑ > 1)
                {
                    ϑ = 0; ϐ++; if (ϐ > 1) ϐ = 0; bool[] Ϟ = new bool[]
                    {Ц.Count==0,ĥ==И.ɗ,Ф.Count==0}; int Ä = 0; while (true) { Ä++; Ϗ++; if (Ϗ > Ϟ.Length - 1) Ϗ = 0; if (Ä >= Ϟ.Length) break; if (!Ϟ[Ϗ]) break; }
                }
                bool
                    O = ƽ == В.Ͼ; if (!O && ɿ && Ƈ != -1 && ϐ == 0) ϟ = Ƈ < 1000000 ? Math.Round(Ƈ) + " Kg" : Math.Round(Ƈ / 1000) + " t"; if (ύ(ref Ɨ, ƴ, A++, ƛ, " Stop!"))
                {
                    Ҕ();
                    Љ(ϸ.Ϸ);
                }
                if (ύ(ref Ɨ, ƴ, A++, ƛ, " Behavior settings")) if (!O) Љ(ϸ.ϱ); else Љ(ϸ.Џ); if (!O)
                {
                    if (ύ(ref Ɨ, ƴ, A++, ƛ, " Next hole")) Ά(false
                    );
                }
                else if (ύ(ref Ɨ, ƴ, A++, ƛ, " Undock")) Μ = true; Ɨ += ƶ; if (!O) Ɨ += "Progress: " + Math.Round(ӎ, 1) + " %\n"; Ɨ += "State: " + ϕ(Ҡ) + " " + Ϣ +
                    "m \n"; Ɨ += "Load: " + ŏ + " % Max: " + ϟ + " \n"; if (Ϗ == 0) Ɨ += "Uranium: " + (Ц.Count == 0 ? "No reactors" : Math.Round(Ĺ, 1) + "Kg " + (ɷ == -1 ? "" :
                    " Min: " + ɷ + " Kg")) + "\n"; if (Ϗ == 1) Ɨ += "Battery: " + (ĥ == И.ɗ ? ϗ(ĥ) : Ĩ + "% " + (ɸ == -1 || O ? "" : " Min: " + ɸ + " %")) + "\n"; if (Ϗ == 2) Ɨ += "Hydrogen: " + (
                    Ф.Count == 0 ? "No tanks" : Math.Round(ļ, 1) + "% " + (ɶ == -1 || O ? "" : " Min: " + ɶ + " %")) + "\n";
            }
            else if (Є == ϸ.ϱ)
            {
                String Ι = ""; if (ύ(ref Ι, ƴ,
                    A++, ƛ, " Back")) { if (Ӄ == ӌ.Ӊ) Љ(ϸ.ϳ); else Љ(ϸ.Ϸ); }
                if (ύ(ref Ι, ƴ, A++, ƛ, " Max load: " + ɹ + "%")) ʈ(ref ɹ, ɹ <= 80 ? -10 : -5, ʉ.Ă, false); if (
                    ύ(ref Ι, ƴ, A++, ƛ, " Weight limit: " + (ɿ ? "On" : "Off"))) ɿ = !ɿ; if (ύ(ref Ι, ƴ, A++, ƛ, " Ejection: " + Ϙ(ɻ))) { ɻ = ͼ(ɻ); }
                if (ύ(ref Ι, ƴ, A++, ƛ
                    , " Toggle sorters: " + (ɽ ? "On" : "Off"))) { ɽ = !ɽ; if (ɽ) Ũ(ũ); }
                if (ύ(ref Ι, ƴ, A++, ƛ, " Unload ice: " + (ɺ ? "On" : "Off"))) ɺ = !ɺ; if (ύ(ref Ι,
                    ƴ, A++, ƛ, " Uranium: " + (ɷ == -1 ? "Ignore" : "Min " + ɷ + "Kg"))) ʈ(ref ɷ, (ɷ > 5 ? -5 : -1), ʉ.ʏ, true); if (ύ(ref Ι, ƴ, A++, ƛ, " Battery: " + (ɸ == -1
                    ? "Ignore" : "Min " + ɸ + "%"))) ʈ(ref ɸ, -5, ʉ.ʐ, true); if (ύ(ref Ι, ƴ, A++, ƛ, " Hydrogen: " + (ɶ == -1 ? "Ignore" : "Min " + ɶ + "%"))) ʈ(ref ɶ, -10
                    , ʉ.ʎ, true); if (ύ(ref Ι, ƴ, A++, ƛ, " When done: " + (ʅ ? "Return home" : "Stop"))) ʅ = !ʅ; if (ύ(ref Ι, ƴ, A++, ƛ, " On damage: " + Ϛ(ʆ)))
                {
                    ʆ = ͼ(
                    ʆ);
                }
                if (ύ(ref Ι, ƴ, A++, ƛ, " Advanced...")) Љ(ϸ.ϰ); Ɨ += ǧ(8, Ι, ƴ, ref Ϥ);
            }
            else if (Є == ϸ.ϰ)
            {
                if (ύ(ref Ɨ, ƴ, A++, ƛ, " Back"))
                {
                    if (Ӄ == ӌ.Ӊ) Љ
                    (ϸ.ϳ);
                    else Љ(ϸ.Ϸ);
                }
                if (ύ(ref Ɨ, ƴ, A++, ƛ, (ƽ == В.ψ ? " Grinder" : " Drill") + " inv. balancing: " + (ɾ ? "On" : "Off"))) ɾ = !ɾ; if (ύ(ref Ɨ, ƴ,
                    A++, ƛ, " Enable" + (ƽ == В.ψ ? " grinders" : " drills") + ": " + (ʇ ? "Fwd + Bwd" : "Fwd"))) ʇ = !ʇ; if (ύ(ref Ɨ, ƴ, A++, ƛ, " Work speed fwd.: " + ɴ
                    + "m/s")) ʈ(ref ɴ, 0.5f, ʉ.ʓ, false); if (ύ(ref Ɨ, ƴ, A++, ƛ, " Work speed bwd.: " + ɳ + "m/s")) ʈ(ref ɳ, 0.5f, ʉ.ʓ, false); if (ύ(ref Ɨ, ƴ, A++
                    , ƛ, " Acceleration: " + Math.Round(ɵ * 100f) + "%" + (ɵ > 0.80f ? " (risky)" : ""))) { ʈ(ref ɵ, 0.1f, ʉ.ª, false); }
                if (ύ(ref Ɨ, ƴ, A++, ƛ,
                    " Width overlap: " + ɼ * 100f + "%")) ʠ(true, 0.05f); if (ύ(ref Ɨ, ƴ, A++, ƛ, " Height overlap: " + ɲ * 100f + "%")) ʠ(false, 0.05f);
            }
            else if (Є == ϸ.Џ)
            {
                if (ύ(ref Ɨ
                    , ƴ, A++, ƛ, " Back")) { if (Ӄ == ӌ.Ӊ) Љ(ϸ.ϳ); else Љ(ϸ.Ϸ); }
                if (ύ(ref Ɨ, ƴ, A++, ƛ, " Max load: " + ɹ + "%")) ʈ(ref ɹ, ɹ <= 80 ? -10 : -5, ʉ.Ă, false);
                if (ύ(ref Ɨ, ƴ, A++, ƛ, " Unload ice: " + (ɺ ? "On" : "Off"))) ɺ = !ɺ; if (ύ(ref Ɨ, ƴ, A++, ƛ, " Uranium: " + (ɷ == -1 ? "Ignore" : "Min " + ɷ + "Kg"))) ʈ(
                ref ɷ, (ɷ > 5 ? -5 : -1), ʉ.ʏ, true); if (ύ(ref Ɨ, ƴ, A++, ƛ, " Battery: " + (ɸ == -1 ? "Ignore" : "Charge up"))) ɸ = (ɸ == -1 ? 1 : -1); if (ύ(ref Ɨ, ƴ, A++, ƛ
                , " Hydrogen: " + (ɶ == -1 ? "Ignore" : "Fill up"))) ɶ = (ɶ == -1 ? 1 : -1); if (ύ(ref Ɨ, ƴ, A++, ƛ, " On damage: " + Ϛ(ʆ))) { ʆ = ͼ(ʆ); }
                if (ύ(ref Ɨ, ƴ, A
                ++, ƛ, " Acceleration: " + Math.Round(ɵ * 100f) + "%" + (ɵ > 0.80f ? " (risky)" : ""))) { ʈ(ref ɵ, 0.1f, ʉ.ª, false); }
            }
            else if (Є == ϸ.ϵ)
            {
                double Ʌ
                = 0; if (ˋ.Count > 0) Ʌ = Vector3.Distance(ˋ.Last().ɉ, ɉ); if (ύ(ref Ɨ, ƴ, A++, ƛ, " Stop path recording")) ˎ(); if (ƽ != В.Ͼ)
                {
                    if (ύ(ref Ɨ, ƴ, A
                ++, ƛ, " Home: " + (Ώ ? "Use old home" : (Β.Ͷ ? "Was set! " : "none ")))) Ώ = !Ώ;
                }
                else
                {
                    if (ύ(ref Ɨ, ƴ, A++, ƛ, " Connector 1: " + (Ώ ?
                "Use old connector" : (Β.Ͷ ? "Was set! " : "none ")))) Ώ = !Ώ; if (ύ(ref Ɨ, ƴ, A++, ƛ, " Connector 2: " + (Ύ ? "Use old connector" : (Ǟ.Ͷ ? "Was set! " : "none ")))
                ) Ύ = !Ύ;
                }
                if (ύ(ref Ɨ, ƴ, A++, ƛ, " Path: " + (Ό ? "Use old path" : (ˋ.Count > 1 ? "Count: " + ˋ.Count : "none ")))) Ό = !Ό; Ɨ += ƶ; Ɨ += "Wp spacing: "
                + Math.Round(ˈ) + "m\n";
            }
            else if (Є == ϸ.ϲ)
            {
                if (ύ(ref Ɨ, ƴ, A++, ƛ, " Stop")) { Ҕ(); Љ(ϸ.Ϸ); }
                Ɨ += ƶ; Ɨ += "State: " + ϕ(Ҡ) + " \n"; Ɨ += "Speed: " +
                Math.Round(κ, 1) + "m/s\n"; ; Ɨ += "Target dist: " + Ϣ + "m\n"; Ɨ += "Wp count: " + ˋ.Count + "\n"; Ɨ += "Wp left: " + Ԋ + "\n";
            }
            else if (Є == ϸ.ϯ)
            {
                List
                <IMyTerminalBlock> ʢ = į(); if (ο) ϑ++; if (ϑ >= ʢ.Count) ϑ = 0; if (ύ(ref Ɨ, ƴ, A++, ƛ, " Next")) Љ(ϸ.Ϯ); Ɨ += ƶ; Ɨ += "Version: " + VERSION + "\n"; Ɨ
                += "Ship load: " + Math.Round(ŏ, 1) + "% " + Math.Round(œ, 1) + " / " + Math.Round(ł, 1) + "\n"; Ɨ += "Uranium: " + (Ц.Count == 0 ? "No reactors" :
                Math.Round(Ĺ, 1) + "Kg " + ĸ) + "\n"; Ɨ += "Battery: " + (ĥ == И.ɗ ? "" : Ĩ + "% ") + ϗ(ĥ) + "\n"; Ɨ += "Hydrogen: " + (Ф.Count == 0 ? "No tanks" : Math.Round(
                ļ, 1) + "% ") + "\n"; Ɨ += "Damage: " + (ʢ.Count == 0 ? "None" : "" + (ϑ + 1) + "/" + ʢ.Count + " " + ʢ[ϑ].CustomName) + "\n";
            }
            else if (Є == ϸ.Ϯ)
            {
                if (ύ(ref
                Ɨ, ƴ, A++, ƛ, " Back")) Љ(ϸ.Ϸ); Ɨ += ƶ; Ɨ += "Next scan: " + Ϊ + "s\n"; Ɨ += "Ship size: " + Math.Round(Й, 1) + "m " + Math.Round(Я, 1) + "m " + Math.
                Round(Ю, 1) + "m \n"; Ɨ += "Broadcast: " + (ɯ ? "Online - " + ɮ : "Offline") + "\n"; Ɨ += "Max Instructions: " + Math.Round(Ψ * 100f, 1) + "% \n";
            }
            else
                if (Є == ϸ.ϭ)
            {
                if (ύ(ref Ɨ, ƴ, A++, ƛ, " Back")) Љ(ϸ.Ϸ); Ɨ += ƶ; Ɨ += "1. Dock to your docking station\n"; Ɨ +=
                "2. Select Record path & set home\n"; Ɨ += "3. Fly the path to the ores\n"; Ɨ += "4. Select stop path recording\n"; Ɨ += "5. Align ship in mining direction\n"; Ɨ +=
                "6. Select Setup job and start\n";
            }
            if (ɘ == 2) Ɨ = "Fatal setup error\nNext scan: " + Ϊ + "s\n"; if (ɜ) Ɨ = "Recompile script now"; int ã = Ɨ.Split('\n').Length; for (int ʡ =
                ã; ʡ <= 10; ʡ++) Ɨ += "\n"; Ɨ += Ʃ; Ɨ += "Last: " + ώ + "\n"; return Ɨ;
        }
        void ʠ(bool ʟ, float ʗ)
        {
            Ҕ(); Қ(true, false); if (ʟ) ʈ(ref ɼ, ʗ, ʉ.ʍ, false);
            else ʈ(ref ɲ, ʗ, ʉ.ʍ, false); Ѳ(); Ş(true, true, 0, 0);
        }
        float ʞ(float ʝ, float[] ʜ)
        {
            float J = ʜ[0]; for (int ʛ = ʜ.Length - 1; ʛ >= 0; ʛ--) if (ʝ < ʜ[
            ʛ]) J = ʜ[ʛ]; return J;
        }
        String ʙ(ref int E) { String O = ""; if (E >= Ы.Count) E = -1; if (E >= 0) { O = Ы[E].CustomName; } E++; return O; }
        void ʘ(
            string Ô)
        {
            if (Ӄ != ӌ.Ӊ) return; if (Ô == "") return; IMyTerminalBlock q = χ.GetBlockWithName(Ô); if (q == null || !(q is IMyTimerBlock))
            {
                ώ =
            "Timerblock " + Ô + " not found!"; return;
            } ((IMyTimerBlock)q).Trigger();
        }
        void ő(ref int ʗ, int ʖ, int ʚ, int ʕ)
        {
            if (ʖ == 0) return; if (ʗ < ʚ && ʕ > 0 || ʗ
            <= ʚ && ʕ < 0) { ʗ += ʕ; return; }
            int ʤ = Math.Abs(ʖ); int ĝ = 0; int ʹ = 1; while (true)
            {
                ĝ += ʹ * ʤ * 10; if (ʖ < 0 && ʗ - ʚ <= ĝ) break; if (ʖ > 0 && ʗ - ʚ < ĝ) break; ʹ
            ++;
            }
            ʗ += ʹ * ʖ;
        }
        void ʸ(bool ʷ) { ʃ = Math.Max(ʃ, 1); ʂ = Math.Max(ʂ, 1); ʁ = Math.Max(ʁ, 0); Ş(Є == ϸ.ϴ, false, 0, 0); }
        public enum ʶ { ʵ, ʴ, ʳ }
        public
            enum ʲ
        { ʱ, ʰ }
        public enum ʯ { ʮ, ʭ, ʬ, ʫ }
        public enum ʪ { ɗ, ʩ, ʨ, ʧ, ʦ, ʥ, ʺ }
        ų ʔ = new ų(); ų ʒ = new ų(); ʯ ʆ = ʯ.ʮ; bool ʅ = true; ʲ ʄ = ʲ.ʱ; int ʃ = 3; int
            ʂ = 3; int ʁ = 30; ʶ ʀ = ʶ.ʵ; bool ɿ = true; bool ɾ = true; bool ʇ = true; bool ɽ = false; ʪ ɻ = ʪ.ɗ; bool ɺ = true; float ɹ = 90; float ɸ = 20; float ɷ = 5
            ; float ɶ = 20; float ɵ = 0.70f; float ɴ = 1.50f; float ɳ = 2.50f; float ɼ = 0f; float ɲ = 0f; public enum ʉ { ʓ, ª, Ă, ʑ, ʐ, ʏ, ʎ, K, ʍ }; bool ʈ(ref
            float A, bool ʌ, ʉ ž, String O, String ʊ)
        {
            if (O == "") return false; float J = -1; bool ʋ = false; if (O.ToUpper() == ʊ) ʋ = true;
            else if (!float.
            TryParse(O, out J)) return false;
            else J = Math.Max(0, J); if (ʌ) J = (float)Math.Round(J); ʈ(ref A, J, ž, ʋ, false); return true;
        }
        void ʈ(ref
            float A, float ő, ʉ ž, bool ʊ)
        { ʈ(ref A, A + ő, ž, ʊ, true); }
        void ʈ(ref float A, float ʻ, ʉ ž, bool ʊ, bool Έ)
        {
            float ĝ = 0; float ȵ = 0; if (ž == ʉ.
            ʓ) { ȵ = 0.5f; ĝ = 10f; }
            if (ž == ʉ.ª) { ȵ = 0.1f; ĝ = 1f; }
            if (ž == ʉ.ʑ) { ȵ = 50f; ĝ = 100f; }
            if (ž == ʉ.ʐ) { ȵ = 5f; ĝ = 30f; }
            if (ž == ʉ.ʏ) { ȵ = 1f; ĝ = 25f; }
            if (ž == ʉ.Ă
            ) { ȵ = 10f; ĝ = 95f; }
            if (ž == ʉ.ʎ) { ȵ = 10f; ĝ = 90f; }
            if (ž == ʉ.K) { ȵ = 10f; ĝ = 1800; }
            if (ž == ʉ.ʍ) { ȵ = 0.0f; ĝ = 0.75f; }
            if (ʻ == -1 && ʊ) { A = -1; return; }
            if (A
            == -1) ʊ = false; bool Ç = ʻ < ȵ || ʻ > ĝ; if (Ç && Έ) { if (ʻ < A) A = ĝ; else if (ʻ > A) A = ȵ; } else A = ʻ; if (Ç && ʊ) A = -1; else A = Math.Max(ȵ, Math.Min(A, ĝ)); A
            = (float)Math.Round(A, 2);
        }
        void Ά(bool ͽ) { if (ͽ) ԑ = Math.Max(0, ԑ - 1); else ԑ++; З(true); }
        ŕ ͼ<ŕ>(ŕ ͺ)
        {
            int ª = Array.IndexOf(Enum.
            GetValues(ͺ.GetType()), ͺ); ª++; if (ª >= ͻ(ͺ)) ª = 0; return (ŕ)Enum.GetValues(ͺ.GetType()).GetValue(ª);
        }
        int ͻ<ŕ>(ŕ ͺ)
        {
            return Enum.
            GetValues(ͺ.GetType()).Length;
        }
        class ͷ
        {
            public bool Ͷ = false; public Vector3 ɉ = new Vector3(); public Vector3 ç = new Vector3(); public
            Vector3 Ï = new Vector3(); public Vector3 ĕ = new Vector3(); public Vector3 Í = new Vector3(); public Vector3 Ή = new Vector3(); public
            float Ί = 0; public float Η = 0; public float[] Θ = null; public ͷ() { }
            public ͷ(ͷ Ζ) { Ͷ = Ζ.Ͷ; ɉ = Ζ.ɉ; ç = Ζ.ç; Ï = Ζ.Ï; ĕ = Ζ.ĕ; Í = Ζ.Í; Ή = Ζ.Ή; Θ = Ζ.Θ; }
            public ͷ(Vector3 ɉ, Vector3 Ï, Vector3 ç, Vector3 ĕ, Vector3 Í) { this.ɉ = ɉ; this.ç = ç; this.Ï = Ï; this.ĕ = ĕ; this.Ί = 0; this.Í = Í; }
            public void
            Ε(List<IMyThrust> Δ, List<string> Γ)
            {
                Θ = new float[Γ.Count]; for (int A = 0; A < Θ.Length; A++) Θ[A] = -1; for (int A = 0; A < Δ.Count; A++)
                {
                    string O = L(Δ[A]); int E = Γ.IndexOf(O); if (E != -1) Θ[E] = µ(Δ[A].MaxEffectiveThrust, Δ[A].MaxThrust);
                }
            }
        }
        ͷ Β = new ͷ(); ͷ Α = new ͷ(); ͷ ΐ = new
                    ͷ(); bool Ώ = false; bool Ύ = false; bool Ό = false; void ʹ()
        {
            ˊ.Clear(); for (int A = 0; A < ˋ.Count; A++) ˊ.Add(ˋ[A]); ˋ.Clear(); ˍ = true; Α =
                    new ͷ(Β); ΐ = new ͷ(Ǟ); Β.Ͷ = false; if (ƽ == В.Ͼ) Ǟ.Ͷ = false; for (int A = 0; A < ä.Count; A++) if (!ˌ.Contains(ä.Keys.ElementAt(A))) ˌ.Add(ä.
                    Keys.ElementAt(A)); Ώ = false; Ύ = false; Ό = false; Љ(ϸ.ϵ);
        }
        void ˎ()
        {
            if (Ώ) Β = Α; if (Ύ) Ǟ = ΐ; if (Ό)
            {
                ˋ.Clear(); for (int A = 0; A < ˊ.Count; A++) ˋ.
                    Add(ˊ[A]);
            }
            ˍ = false; Ҕ(); Љ(ϸ.Ϸ);
        }
        bool ˍ = false; List<String> ˌ = new List<string>(); List<ͷ> ˋ = new List<ͷ>(); List<ͷ> ˊ = new List<ͷ>();
        int ˉ = 0; double ˈ = 0; void ˇ()
        {
            if (!ˍ) return; if (Ҡ != ҍ.Д) { ˎ(); return; }
            if (!Α.Ͷ) Ώ = false; if (!ΐ.Ͷ) Ύ = false; if (ˊ.Count <= 1) Ό = false;
            IMyShipConnector º = Ò(MyShipConnectorStatus.Connectable); if (º == null) º = Ò(MyShipConnectorStatus.Connected); if (º != null)
            {
                if (Math.Round(κ, 2) <=
            0.20) ˉ++;
                else ˉ = 0; if (ˉ >= 5)
                {
                    if (ƽ == В.Ͼ && (Β.Ͷ || Ώ) && Vector3.Distance(Β.ɉ, º.GetPosition()) > 5)
                    {
                        Ǟ.Ï = Э.WorldMatrix.Forward; Ǟ.ĕ = Э.
            WorldMatrix.Left; Ǟ.ç = Э.WorldMatrix.Down; Ǟ.Í = Э.GetNaturalGravity(); Ǟ.ɉ = º.GetPosition(); Ǟ.Ͷ = true; Ǟ.Ή = º.Position;
                    }
                    else
                    {
                        Β.Ï = Э.
            WorldMatrix.Forward; Β.ĕ = Э.WorldMatrix.Left; Β.ç = Э.WorldMatrix.Down; Β.Í = Э.GetNaturalGravity(); Β.ɉ = º.GetPosition(); Β.Ͷ = true; Β.Ή = º.
            Position;
                    }
                }
            }
            double ˆ = -1; if (ˋ.Count > 0) { ˆ = Vector3.Distance(ɉ, ˋ.Last().ɉ); }
            double ğ = Math.Max(1.5, Math.Pow(κ / 100.0, 2)); double ˁ = Math
            .Max(κ * ğ, 2); ˈ = ˁ; if ((ˆ == -1) || ˆ >= ˁ) { ͷ B = new ͷ(ɉ, υ, σ, τ, Э.GetNaturalGravity()); B.Ε(Δ, ˌ); ˋ.Add(B); }
        }
        int ˀ(Vector3 ý, int ʿ)
        {
            if (
            ʿ == -1) return 0; double ʾ = -1; int ʽ = -1; for (int A = ˋ.Count - 1; A >= 0; A--)
            {
                double Ʌ = Vector3.Distance(ˋ[A].ɉ, ý); if (ʾ == -1 || Ʌ < ʾ)
                {
                    ʽ = A;
                    ʾ = Ʌ;
                }
            }
            return Math.Sign(ʽ - ʿ);
        }
        bool ʼ(Vector3 ɉ)
        {
            List<Vector3> J = new List<Vector3>(); for (int A = 0; A < ˋ.Count; A++)
            {
                J.Add(ˋ[A].ɉ
                    );
            }
            if (Β.Ͷ && ˋ.Count >= 1)
            {
                Vector3 ͳ = new Vector3(); ˠ(Β, dockDist * Ю, false, out ͳ); if (Vector3.Distance(Β.ɉ, ˋ.First().ɉ) < Vector3.
                    Distance(Β.ɉ, ˋ.Last().ɉ)) { J.Insert(0, ͳ); J.Insert(0, Β.ɉ); }
                else { J.Add(ͳ); J.Add(Β.ɉ); }
            }
            if (ƽ == В.Ͼ)
            {
                if (Ǟ.Ͷ && ˋ.Count >= 1)
                {
                    Vector3 Ͳ = new
                    Vector3(); ˠ(Ǟ, dockDist * Ю, false, out Ͳ); if (Vector3.Distance(Ǟ.ɉ, ˋ.First().ɉ) < Vector3.Distance(Ǟ.ɉ, ˋ.Last().ɉ))
                    {
                        J.Insert(0, Ͳ); J.
                    Insert(0, Ǟ.ɉ);
                    }
                    else { J.Add(Ͳ); J.Add(Ǟ.ɉ); }
                }
            }
            else
            {
                if (Ӄ != ӌ.Ӌ) if (ˋ.Count > 0 && Vector3.Distance(Ǟ.ɉ, ˋ.First().ɉ) < Vector3.Distance(Ǟ.ɉ
                    , ˋ.Last().ɉ)) J.Insert(0, Ǟ.ɉ);
                    else J.Add(Ǟ.ɉ);
            }
            int ʽ = -1; double ͱ = -1; for (int A = 0; A < J.Count; A++)
            {
                double Ʌ = Vector3.Distance(J
                    [A], ɉ); if (Ʌ < ͱ || ͱ == -1) { ͱ = Ʌ; ʽ = A; }
            }
            if (J.Count == 0) return false; double Ͱ = Vector3.Distance(J[ʽ], ɉ); double ˮ = Vector3.Distance(J[
                    Math.Max(0, ʽ - 1)], J[ʽ]) * 1.5f; double ˬ = Vector3.Distance(J[Math.Min(J.Count - 1, ʽ + 1)], J[ʽ]) * 1.5f; return Ͱ < ˮ || Ͱ < ˬ;
        }
        ͷ ˤ = null; void ˣ
                    (ͷ B, ӌ ˢ)
        { ˤ = B; if (Ӄ == ӌ.Ӊ) ҟ = ˢ; }
        ͷ ˡ() { if (ƽ != В.Ͼ) return Β; return ˤ; }
        bool ˠ(ͷ ˑ, float Ʌ, bool ː, out Vector3 ˏ)
        {
            if (ː)
            {
                Vector3I Ƌ
                    = new Vector3I((int)ˑ.Ή.X, (int)ˑ.Ή.Y, (int)ˑ.Ή.Z); IMySlimBlock ʣ = Me.CubeGrid.GetCubeBlock(Ƌ); if (ʣ == null || !(ʣ.FatBlock is
                    IMyShipConnector)) { ˏ = new Vector3(); return false; }
                Vector3 đ = Ȑ(Э, ʣ.FatBlock.GetPosition() - ɉ); Vector3 м = Ȑ(Э, ʣ.FatBlock.WorldMatrix.Forward)
                    ; ˏ = ˑ.ɉ - Ȏ(ˑ.Ï, ˑ.ç * -1, đ) - Ȏ(ˑ.Ï, ˑ.ç * -1, м) * Ʌ; return true;
            }
            else { ˏ = ˑ.ɉ; return true; }
        }
        Vector3 ҿ = new Vector3(); bool ҽ = false;
        Vector3 Ҽ(int Y, int X, bool һ)
        {
            if (!һ && ҽ) return ҿ; float Ă = ((Ӂ - 1f) / 2f) - Y; float K = ((Ӏ - 1f) / 2f) - X; ҿ = Ǟ.ɉ + Ǟ.ĕ * Ă * Й + ӄ * -1 * K * Я; ҽ = true;
            return ҿ;
        }
        Vector3 Һ(Vector3 ҹ, float Ҿ) { return ҹ + (Ӆ * Ҿ); }
        public enum Ҹ { ҷ, Ҷ, ґ, Ҍ, ҵ, Ҵ }
        Ҹ ҳ()
        {
            float Ʌ = -1; Ҹ ª = Ҹ.ҷ; if (ƽ != В.Ͼ)
            {
                if (Ӄ != ӌ.Ӌ
            )
                {
                    Vector3 ț = ȕ(Ӆ, ӄ * -1, ɉ - Ǟ.ɉ); if (Math.Abs(ț.X) <= (float)(Ӂ * Й) / 2f && Math.Abs(ț.Y) <= (float)(Ӏ * Я) / 2f)
                    {
                        if (ț.Z <= -1 && ț.Z >= -Ҍ * 2)
                            return Ҹ.Ҍ; if (ț.Z > -1 && ț.Z < Ю * 2) return Ҹ.ҷ;
                    }
                    if (Ӎ(Ǟ.ɉ, ref Ʌ)) ª = Ҹ.ҷ;
                }
                if (Β.Ͷ)
                {
                    if (Ӎ(Β.ɉ, ref Ʌ)) ª = Ҹ.ґ; for (int A = 0; A < ˋ.Count; A++)
                    {
                        if (Ӎ
                            (ˋ[A].ɉ, ref Ʌ)) ª = Ҹ.Ҷ;
                    }
                    if (Vector3.Distance(ɉ, Β.ɉ) < dockDist * Ю) ª = Ҹ.ґ; if (Ò(MyShipConnectorStatus.Connectable) != null || Ò(
                            MyShipConnectorStatus.Connected) != null) ª = Ҹ.ґ;
                }
            }
            else
            {
                Vector3 ɉ = new Vector3(); IMyShipConnector Ä = Ò(MyShipConnectorStatus.Connected); if (Β.Ͷ)
                {
                    if (
                            Ӎ(Β.ɉ, ref Ʌ)) ª = Ҹ.ґ; if (ˠ(Β, dockDist, true, out ɉ)) if (Ӎ(ɉ, ref Ʌ)) ª = Ҹ.ґ; if (Ä != null && Vector3.Distance(Ä.GetPosition(), Β.ɉ) < 5)
                        return Ҹ.ҵ;
                }
                for (int A = 0; A < ˋ.Count; A++) if (Vector3.Distance(ˋ[A].ɉ, Β.ɉ) > dockDist * Ю && Vector3.Distance(ˋ[A].ɉ, Ǟ.ɉ) > dockDist * Ю) if (Ӎ
                        (ˋ[A].ɉ, ref Ʌ)) ª = Ҹ.Ҷ; if (Ǟ.Ͷ)
                {
                    if (Ӎ(Ǟ.ɉ, ref Ʌ)) ª = Ҹ.ҷ; if (ˠ(Ǟ, dockDist, true, out ɉ)) if (Ӎ(ɉ, ref Ʌ)) ª = Ҹ.ҷ; if (Ä != null && Vector3.
                        Distance(Ä.GetPosition(), Ǟ.ɉ) < 5) return Ҹ.Ҵ;
                }
            }
            return ª;
        }
        bool Ӎ(Vector3 ø, ref float Ʌ)
        {
            float ț = Vector3.Distance(ø, ɉ); if (ț < Ʌ || Ʌ == -1
                        ) { Ʌ = ț; return true; }
            return false;
        }
        public enum ӌ { Ӌ, ӊ, Ӊ, ΰ, Ғ, ғ, ӈ, Ӈ, ӆ }
        ͷ Ǟ = new ͷ(); Vector3 Ӆ; Vector3 ӄ; ӌ Ӄ = ӌ.Ӌ; ʲ ӂ = ʲ.ʱ; int Ӂ = 0;
        int Ӏ = 0; double ӎ = 0; bool Ҳ = false; void Ұ()
        {
            if (ɘ > 0) { ώ = "Setup error! Can't start"; return; }
            if (ƽ == В.Ͼ) { қ(); return; }
            Ǟ.ɉ = ɉ; Ǟ.Í = Э.
        GetNaturalGravity(); Ǟ.Ï = υ; Ǟ.ç = σ; Ǟ.ĕ = τ; Ӆ = М.WorldMatrix.Forward; ӄ = Ǟ.ç; if (Ӆ == Э.WorldMatrix.Down) ӄ = Э.WorldMatrix.Backward; Қ(true, true); Ҝ(ҍ.ʟ)
        ; ҕ();
        }
        void Қ(bool Ç, bool ҙ)
        {
            if (Ӄ == ӌ.Ӌ && !Ç) return; bool Ҙ = Ç || Ӄ == ӌ.ΰ || Ӂ != ʃ || Ӏ != ʂ || ӂ != ʄ; if (Ҙ)
            {
                if (Ӄ != ӌ.Ӌ)
                {
                    Ӄ = ӌ.Ғ; Ҽ(ԓ, Ԓ, ҙ); ώ =
        "Job changed, lost progress";
                }
                ӂ = ʄ; Ӂ = ʃ; Ӏ = ʂ; Ԓ = 0; ԓ = 0; Ԁ = 0; Ԑ = 0; ԁ = 0; ԑ = 0; З(true);
            }
        }
        void җ() { ñ(ɉ, 0); Ŗ(Δ, true); }
        int Җ = 0; void ҕ()
        {
            Ͻ(Ѓ.Ђ); җ(); ť(Ч, false); Ũ(ũ); Ӄ
        = ӌ.Ӊ; È(true); ҟ = Ӄ; Љ(ϸ.ϳ); К(); Ҳ = true; Җ = 0; for (int A = У.Count - 1; A >= 0; A--) if (ƃ(У[A], false)) Җ++; if (Җ > 0) ώ = "Started with damage";
        }
        void Ҕ()
        {
            if (Ӄ == ӌ.Ӊ) { Ӄ = ӌ.ӊ; ώ = "Job paused"; }
            Ҝ(ҍ.Д); ҟ = Ӄ; á(false, 0, 0, 0, 0); å(); k(new Vector3(), false); ì(); ŧ(ChargeMode.Auto); Ŕ(
        false); ű(true); Ӿ(ҍ.Д); Ş(false, false, 0, 0); Ŗ(Ш, false); Ŗ(Ъ, true); Ũ(true); ԋ = false; Ҳ = false; Ɔ = false; Μ = false; if (Є != ϸ.Ϸ && Є != ϸ.ϱ && Є != ϸ
        .ϰ && Є != ϸ.Џ) Љ(ϸ.Ϸ);
        }
        void қ()
        {
            Ҹ Ҏ = ҳ(); if (ƽ == В.Ͼ)
            {
                if (!Ǟ.Ͷ || !Β.Ͷ) return; ҕ(); bool ғ = Vector3.Distance(ɉ, Β.ɉ) < Vector3.Distance(ɉ
        , Ǟ.ɉ); if (ɛ == ӌ.Ӈ) ғ = true; if (ɛ == ӌ.ӆ) ғ = false; if (ғ)
                {
                    ˣ(Β, ӌ.Ӈ); switch (Ҏ)
                    {
                        case Ҹ.ҵ: Ҝ(ҍ.ҡ); break;
                        case Ҹ.Ҷ: Ҝ(ҍ.Ү); break;
                        case Ҹ.ґ:
                            Ҝ(
        ҍ.Ҭ); break;
                        default: Ҝ(ҍ.ҧ); break;
                    }
                }
                else
                {
                    ˣ(Ǟ, ӌ.ӆ); switch (Ҏ)
                    {
                        case Ҹ.Ҵ: Ҝ(ҍ.ҡ); break;
                        case Ҹ.ҷ: Ҝ(ҍ.Ҭ); break;
                        case Ҹ.Ҷ:
                            Ҝ(ҍ.Ү);
                            break;
                        default: Ҝ(ҍ.ҧ); break;
                    }
                }
            }
            else
            {
                if (Ӄ != ӌ.ӊ && Ӄ != ӌ.Ғ) return; bool Ғ = Ӄ == ӌ.Ғ; ҕ(); bool ґ = Ű(false) && Β.Ͷ; switch (Ҏ)
                {
                    case Ҹ.ҷ:
                        Ҝ(ґ ? ҍ.Ҧ
                            : ҍ.ʟ); break;
                    case Ҹ.Ҷ: Ҝ(ґ ? ҍ.ү : ҍ.Ү); break;
                    case Ҹ.ґ: Ҝ(ґ ? ҍ.Ҭ : ҍ.Ҩ); break;
                    case Ҹ.Ҍ: { if (Ԑ != ԑ || Ғ) Ҝ(ҍ.ҝ); else Ҝ(ҍ.Ҍ); } break;
                    default: break;
                }
            }
        }
        void Ґ()
        {
            if (Ӄ == ӌ.Ӌ && !Β.Ͷ) return; if (ƽ == В.Ͼ && (!Ǟ.Ͷ || !Β.Ͷ)) return; ώ = "Move to job"; Ҹ Ҏ = ҳ(); if (ƽ == В.Ͼ)
            {
                ˣ(Ǟ, ӌ.ӆ);
                switch (Ҏ) { case Ҹ.ҷ: Ҝ(ҍ.Ҭ); break; case Ҹ.Ҷ: Ҝ(ҍ.Ү); break; case Ҹ.Ҵ: return; default: Ҝ(ҍ.ҧ); break; }
                Ӿ(ҍ.ҡ);
            }
            else
            {
                switch (Ҏ)
                {
                    case Ҹ.ҷ:
                        Ҝ(
                ҍ.Ү); break;
                    case Ҹ.Ҷ: Ҝ(ҍ.Ү); break;
                    case Ҹ.ґ: Ҝ(ҍ.Ҩ); break;
                    case Ҹ.Ҍ: Ҝ(ҍ.ҝ); break;
                    default: break;
                }
                if (Ӄ == ӌ.Ӌ) Ӿ(ҍ.Ү); else Ӿ(ҍ.Ҫ);
                ԋ = true;
            }
            җ(); Љ(ϸ.ϲ); ť(Ч, false); ҟ = ӌ.ӈ;
        }
        void ҏ()
        {
            if (!Β.Ͷ) return; ώ = "Move home"; Ҹ Ҏ = ҳ(); if (ƽ == В.Ͼ)
            {
                ˣ(Β, ӌ.Ӈ); switch (Ҏ)
                {
                    case Ҹ.Ҷ
                :
                        Ҝ(ҍ.ү); break;
                    case Ҹ.ґ: Ҝ(ҍ.Ҭ); break;
                    case Ҹ.ҵ: return;
                    default: Ҝ(ҍ.ҧ); break;
                }
                Ӿ(ҍ.ҡ);
            }
            else
            {
                if (Ò(MyShipConnectorStatus.
                Connected) != null) return; if (Ò(MyShipConnectorStatus.Connectable) != null) { Ҝ(ҍ.ґ); Ӿ(ҍ.Ҩ); return; }
                switch (Ҏ)
                {
                    case Ҹ.ҷ: Ҝ(ҍ.Ҧ); break;
                    case
                Ҹ.Ҷ:
                        Ҝ(ҍ.ү); break;
                    case Ҹ.ґ: Ҝ(ҍ.ү); break;
                    case Ҹ.Ҍ: Ҝ(ҍ.ұ); break;
                    default: break;
                }
                Ӿ(ҍ.Ҩ);
            }
            җ(); Љ(ϸ.ϲ); ť(Ч, false); ҟ = ӌ.ғ;
        }
        public
                enum ҍ
        { Д, ʟ, Ҍ, ҝ, ұ, ү, Ү, ҭ, Ҭ, ґ, ҫ, Ҫ, ҩ, Ҩ, ҧ, Ҧ, Ŧ, ļ, Ĺ, ҥ, Ҥ, ң, Ң, ҡ, }
        ҍ Ҡ; ӌ ҟ; void Ҝ(ҍ Ҟ)
        {
            if (Ҟ == ҍ.Д) ӏ = ҍ.Д; if (ӏ != ҍ.Д && Ҡ == ӏ && Ҟ != ӏ)
            {
                Ҕ();
                return;
            }
            Ԏ = true; Ҡ = Ҟ;
        }
        ҍ ӏ; void Ӿ(ҍ ӏ) { this.ӏ = ӏ; }
        ӻ Ӽ = null; class ӻ
        {
            public ͷ Ӻ = null; public List<Vector3> ӹ = new List<Vector3>();
            public float Ӹ = 0; public float ӷ = 0; public float Ӷ = 0; public float ӵ = 0; public Vector3 Ӵ = new Vector3();
        }
        public enum ӳ { Ӳ, ӱ, Ӱ }
        int[] ӯ
            = null; ӳ Ӯ(int ӭ, bool Ç)
        {
            if (Ç) { ӯ = null; ; ԓ = 0; Ԓ = 0; }
            if (ʄ == ʲ.ʱ)
            {
                int Ӭ = ӭ + 1; Ԓ = (int)Math.Floor(µ(ӭ, Ӂ)); if (Ԓ % 2 == 0) ԓ = ӭ - (Ԓ * Ӂ);
                else ԓ =
            Ӂ - 1 - (ӭ - (Ԓ * Ӂ)); if (Ԓ >= Ӏ) return ӳ.ӱ; else return ӳ.Ӳ;
            }
            else if (ʄ == ʲ.ʰ)
            {
                if (ӯ == null) ӯ = new int[] { 0, -1, 0, 0 }; int ӫ = (int)Math.
            Ceiling(Ӂ / 2f); int Ӫ = (int)Math.Ceiling(Ӏ / 2f); int ӽ = (int)Math.Floor(Ӂ / 2f); int ӿ = (int)Math.Floor(Ӏ / 2f); int Ԕ = 0; while (ӯ[2] < Math.Pow
            (Math.Max(Ӂ, Ӏ), 2))
                {
                    if (Ԕ > 200) return ӳ.Ӱ; Ԕ++; ӯ[2]++; if (-ӫ < ԓ && ԓ <= ӽ && -Ӫ < Ԓ && Ԓ <= ӿ)
                    {
                        if (ӯ[3] == ӭ)
                        {
                            this.ԓ = ԓ - 1 + ӫ; this.Ԓ = Ԓ - 1 + Ӫ; return
            ӳ.Ӳ;
                        }
                        ӯ[3]++;
                    }
                    if (ԓ == Ԓ || (ԓ < 0 && ԓ == -Ԓ) || (ԓ > 0 && ԓ == 1 - Ԓ)) { int ԕ = ӯ[0]; ӯ[0] = -ӯ[1]; ӯ[1] = ԕ; }
                    ԓ += ӯ[0]; Ԓ += ӯ[1];
                }
            }
            return ӳ.ӱ;
        }
        int ԓ = 0;
        int Ԓ = 0; int ԑ = 0; int Ԑ = 0; int Ҍ = 30; int ԏ = 0; bool Ԏ = true; Vector3 ԍ; double Ԍ = 0; bool ԋ = false; int Ԋ = 0; int ԉ = 0; int Ԉ = 0; int ы = 0; int
        ԇ = 0; Vector3 Ԇ = new Vector3(); float ԅ = 0; float Ԅ = 0; float ԃ = 0; float Ԃ = 0; float ԁ = 0; float Ԁ = 0; bool ө = false; bool ӛ = false; bool ӧ =
        false; bool Ӛ = false; bool ә = false; DateTime ż = new DateTime(); ͷ Ә = null; void ӗ()
        {
            if (Ҡ == ҍ.ʟ)
            {
                if (Ԏ) { ԉ = 0; if (Ԑ != ԑ) { Ԁ = 0; } Ԑ = ԑ; }
                if (ԉ == 0)
                {
                    ӳ J = Ӯ(ԑ, Ԏ); if (J == ӳ.ӱ) { Ӄ = ӌ.ΰ; ώ = "Job done"; if (ʅ && Β.Ͷ) { Ҝ(ҍ.Ҧ); Ӿ(ҍ.Ҩ); ҟ = ӌ.ғ; } else { Ҝ(ҍ.ҭ); Ӿ(ҍ.Ҫ); ҟ = ӌ.ӈ; } return; }
                    if (J == ӳ.Ӳ)
                    {
                        ԉ = 1
                    ; Ŗ(Ш, true); ԍ = Ҽ(ԓ, Ԓ, true); ñ(ԍ, 10); ë(Ǟ.ç, Ǟ.Ï, Ǟ.ĕ, false);
                    }
                }
                else { if (Ԍ < wpReachedDist) { Ҝ(ҍ.Ҍ); return; } }
            }
            if (Ҡ == ҍ.Ҍ)
            {
                if (Ԏ)
                {
                    Ŗ(Ш,
                    true); Ũ(false); ԍ = Ҽ(ԓ, Ԓ, false); ñ(Һ(ԍ, 0), 0); ë(Ǟ.ç, Ǟ.Ï, Ǟ.ĕ, false); ԉ = 1; ԅ = 0; Ԅ = 0; Ԃ = 0; ԃ = -1; Ҍ = ʁ; ө = true;
                }
                if (!Ĳ()) { Ҝ(ҍ.ұ); return; }
                if (Ű
                    (true))
                {
                    ԇ = Ő("", "ORE", ŉ.ņ); if ((ɻ == ʪ.ʧ || ɻ == ʪ.ʦ || ɻ == ʪ.ʥ || ɻ == ʪ.ʺ) && ƽ != В.ψ) Ҝ(ҍ.ң);
                    else if ((ɻ == ʪ.ʩ || ɻ == ʪ.ʨ) && ƽ != В.ψ) Ҝ(ҍ.ҥ);
                    else
                        Ҝ(ҍ.ұ); return;
                }
                ԁ = Vector3.Distance(ɉ, ԍ); if (ԁ > Ԁ) { Ԁ = ԁ; ө = false; }
                if (ƽ == В.ψ && Ж() == MyDetectedEntityType.SmallGrid) Ԅ += 2;
                else Ԅ -= 2
                        ; Ԅ = Math.Max(100, Math.Min(400, Ԅ)); if (ԉ > 0 && ԉ < Ԅ) { if (ԁ > ԅ) { if (Ԅ > 150) ԅ = ԁ; else ԅ = (float)Math.Ceiling(ԁ); ԉ = 1; } else ԉ++; }
                else
                {
                    if (ԉ
                        > 0) { ώ = "Ship stuck! Retrying"; ԅ = ԁ; ԉ = 0; Ş(false, true, 0, Ю * sensorRange); }
                    ñ(Һ(ԍ, Math.Max(0, ԅ - Ю)), Е(false)); if (Ԍ <= wpReachedDist /
                        2) { ԉ = 1; ԅ = 0; }
                    return;
                }
                Ş(false, true, Ю * sensorRange, 0); Vector3 Ӗ = ԍ + Ӆ * ԁ; bool ӕ = false; if (Vector3.Distance(Ӗ, ɉ) > 0.3f)
                {
                    Vector3 Ӕ = ԍ
                        + Ӆ * (ԁ + 0.1f); ñ(Ӕ, 4); ӕ = true;
                }
                else { float κ = Е(true); Vector3 ӓ = Һ(ԍ, Math.Max(ʁ + 1, ԁ + 1)); ñ(true, false, false, ӓ, ӓ - ԍ, κ, κ); }
                bool ΰ =
                        false; if (ʀ == ʶ.ʳ || ʀ == ʶ.ʴ)
                {
                    if (!ӕ)
                    {
                        float Ӓ = 0; foreach (IMyTerminalBlock q in Ш) Ӓ += ō(q, "", "", ʀ == ʶ.ʴ ? new string[] { "STONE" } : null); if (
                        Ӓ > ԃ || ԁ < ʁ || ө) { Ԉ = 0; Ԃ = ԁ; Ҍ = (int)(Math.Max(Ҍ, Ԃ) + Ю / 2); }
                        else { ΰ = ԁ - Ԃ > 2 && Ԉ >= 20; Ԉ++; }
                        ԃ = Ӓ;
                    }
                }
                else ΰ = ԁ >= Ҍ; if (Ԑ != ԑ) { Ҝ(ҍ.ҝ); ԁ = 0; return; }
                if (ΰ) { ԑ++; Ҝ(ҍ.ҝ); ԁ = 0; return; }
            }
            if (Ҡ == ҍ.Ң)
            {
                bool ΰ = false; if (Ԏ)
                {
                    Ũ(true); if ((ɻ == ʪ.ʧ || ɻ == ʪ.ʦ) && ù() && Ȱ(Ӆ, Э.GetNaturalGravity()) <
                25 && Ӂ >= 2 && Ӏ >= 2)
                    {
                        Vector3 ӑ = ɉ; if (ԓ > 0 && Ԓ < Ӏ - 1) ӑ = Ҽ(ԓ - 1, Ԓ + 1, true);
                        else if (ԓ < Ӂ - 1 && Ԓ < Ӏ - 1) ӑ = Ҽ(ԓ + 1, Ԓ + 1, true);
                        else if (ԓ < Ӂ - 1 && Ԓ > 0) ӑ = Ҽ(
                ԓ + 1, Ԓ - 1, true);
                        else if (ԓ > 0 && Ԓ > 0) ӑ = Ҽ(ԓ - 1, Ԓ - 1, true); else ΰ = true; if (!ΰ) ñ(ӑ, 10);
                    }
                    else ΰ = true;
                }
                if (Ԍ < wpReachedDist / 2) ΰ = true; if (ΰ
                ) { Ҝ(ҍ.Ҥ); return; }
            }
            if (Ҡ == ҍ.ҥ || Ҡ == ҍ.Ҥ)
            {
                if (Ԏ) { ñ(true, true, false, ɉ, 0); Ŗ(Ш, false); Ũ(true); ԉ = -1; Ԅ = ɻ == ʪ.ʥ || ɻ == ʪ.ʺ ? 0 : -1; }
                bool J = !
                Ĳ(); int Ń = Ő("STONE", "ORE", ŉ.ņ); if (ɻ == ʪ.ʨ || ɻ == ʪ.ʺ || ɻ == ʪ.ʦ) Ń += Ő("ICE", "ORE", ŉ.ņ); bool Ӑ = Ń > 0; bool ɘ = false; if (Ԅ >= 0)
                {
                    float Y = (
                float)Math.Sin(Ȩ(Ԅ)) * Й / 3f; float X = (float)Math.Cos(Ȩ(Ԅ)) * Я / 3f; Vector3 Ӝ = Ҽ(ԓ, Ԓ, true) + Ȏ(Ӆ, ӄ * -1, new Vector3(Y, X, 0)); ñ(Ӝ, 0.3f); if (
                Ԍ < Math.Min(Й, Я) / 10f) Ԅ += 5f; if (Ԅ >= 360) Ԅ = 0;
                }
                if (ԉ == -1 || Ń < ԉ) { ԉ = Ń; ԅ = 0; } else { ԅ++; if (ԅ > 50) ɘ = true; }
                if (!Ӑ || J || ɘ)
                {
                    if (!J)
                    {
                        int Ө = Ő("",
                "ORE", ŉ.ņ); if (Ű(true)) J = true; else if (100 - (µ(Ө, ԇ) * 100) < minEjection) { J = true; } else Ͻ(Ѓ.Ђ);
                    }
                    if (ɘ && J) ώ = "Ejection failed"; if (Ҡ == ҍ.Ҥ
                ) { if (J) { if (Β.Ͷ) Ҝ(ҍ.Ҧ); else { Ҕ(); Ґ(); ώ = "Can´t return, no dock found"; } } else Ҝ(ҍ.ʟ); }
                    else if (J) Ҝ(ҍ.ұ); else Ҝ(ҍ.Ҍ); return;
                }
            }
            if (Ҡ == ҍ.ҝ || Ҡ == ҍ.ұ || Ҡ == ҍ.ң)
            {
                if (Ԏ)
                {
                    ԍ = Ҽ(ԓ, Ԓ, false); ë(Ǟ.ç, Ǟ.Ï, Ǟ.ĕ, false); Ŗ(Ш, ʇ); Ũ(false); ԅ = Vector3.Distance(ɉ, ԍ); Ş(false, true,
            0, Ю * sensorRange);
                }
                ñ(ԍ, Е(false)); if (Vector3.Distance(ɉ, ԍ) >= ԅ + 5) { Ŗ(Ш, false); Ũ(true); ώ = "Can´t return!"; }
                if (Ԍ < wpReachedDist)
                {
                    if (Ҡ == ҍ.ҝ && ԋ) Ҝ(ҍ.ҭ); if (Ҡ == ҍ.ҝ) Ҝ(ҍ.ʟ); if (Ҡ == ҍ.ң) Ҝ(ҍ.Ң); if (Ҡ == ҍ.ұ)
                    {
                        if (Β.Ͷ) Ҝ(ҍ.Ҧ);
                        else
                        {
                            Ҕ(); Ґ(); ώ =
                    "Can´t return, no dock found";
                        }
                    }
                    return;
                }
            }
            if (Ҡ == ҍ.Ҧ)
            {
                if (Ԏ)
                {
                    Ũ(true); Ŗ(Ш, false); int E = -1; double ӟ = -1; for (int A = ˋ.Count - 1; A >= 0; A--)
                    {
                        double Ʌ = Vector3.
                    Distance(ˋ[A].ɉ, ɉ); if (ӟ == -1 || Ʌ < ӟ) { E = A; ӟ = Ʌ; }
                    }
                    if (E == -1) { Ҝ(ҍ.ү); return; }
                    ĉ = ˋ[E].ɉ; ñ(ĉ, 10); ë(Ǟ.ç, Ǟ.Ï, Ǟ.ĕ, false);
                }
                if (Ԍ < wpReachedDist)
                {
                    Ҝ(ҍ.ү); return;
                }
            }
            if (Ҡ == ҍ.ү || Ҡ == ҍ.Ү)
            {
                if (Ҡ == ҍ.Ү && Ӄ == ӌ.Ӊ && ƽ != В.Ͼ) { if (!Ĳ() || Ű(true)) { Ҝ(ҍ.ү); return; } }
                bool ΰ = false; bool Ӧ = false
                    ; bool ӥ = false; float Ӥ = 0; bool ӣ = false; ͷ B = null; if (Ԏ)
                {
                    if (Ҡ == ҍ.ү || ƽ == В.Ͼ)
                    {
                        ͷ ҋ = ˡ(); Ӽ = new ӻ(); Ӽ.Ӻ = ҋ; Ӽ.Ӹ = followPathDock * Ю; Ӽ.ӷ =
                    useDockDirectionDist * Ю; Ӽ.Ӷ = 10; Ӽ.ӹ.Add(ҋ.ɉ); Vector3 Ӣ = new Vector3(); if (ˠ(ҋ, dockDist * Ю, true, out Ӣ)) Ӽ.ӹ.Add(Ӣ); else Ӽ.Ӹ *= 1.5f; if (ƽ == В.Ͼ)
                        {
                            if (ҋ ==
                    Β) Ӽ.Ӵ = Ǟ.ɉ; if (ҋ == Ǟ) Ӽ.Ӵ = Β.ɉ; Ӽ.ӵ = dockDist * Ю * 1.1f;
                        }
                    }
                    else if (Ҡ == ҍ.Ү)
                    {
                        Ӽ = new ӻ(); Ӽ.Ӻ = Ǟ; Ӽ.Ӹ = followPathJob * Ю; Ӽ.ӷ =
                    useJobDirectionDist * Ю; Ӽ.Ӷ = 10; Ӽ.Ӵ = Β.ɉ; Ӽ.ӵ = dockDist * Ю * 1.1f; Ӽ.ӹ.Add(Ǟ.ɉ); if (Ӄ == ӌ.Ӌ)
                        {
                            if (!Β.Ͷ || ˋ.Count == 0) { Ҕ(); return; }
                            float ӡ = Vector3.Distance(
                    ˋ.First().ɉ, Β.ɉ); float Ӡ = Vector3.Distance(ˋ.Last().ɉ, Β.ɉ); if (ӡ < Ӡ) Ӽ.Ӻ = ˋ.Last(); else Ӽ.Ӻ = ˋ.First();
                        }
                    }
                    Ԇ = new Vector3(); ӣ = !ʼ(ɉ
                    ); Ŗ(Ш, false); Ũ(true); ԏ = -1; double ӟ = -1; for (int A = ˋ.Count - 1; A >= 0; A--)
                    {
                        if (Vector3.Distance(ˋ[A].ɉ, Ӽ.Ӵ) <= Ӽ.ӵ) continue; double
                    Ʌ = Vector3.Distance(ˋ[A].ɉ, ɉ); if (ӟ == -1 || Ʌ < ӟ) { ԏ = A; ӟ = Ʌ; }
                    }
                    ы = ˀ(Ӽ.Ӻ.ɉ, ԏ); Ә = null;
                }
                н(ˋ, ы, Ӽ.ӹ, Ӽ.Ӹ, Ԏ, ref ԉ); for (int A = 0; A < Ӽ.ӹ.Count
                    ; A++) { float Ʌ = Vector3.Distance(ɉ, Ӽ.ӹ[A]); if (Ʌ <= Ӽ.Ӹ) ΰ = true; if (Ʌ <= Ӽ.ӷ) Ӧ = true; }
                if (Ӧ) Ӥ = Ӽ.Ӷ; float Ӟ = Ә != null ? Ә.Ί : κ; float ӝ = (
                    float)Math.Max(κ * 0.1f * Ю, wpReachedDist); if ((Ԍ < ӝ) || Ԏ)
                {
                    if (!Ԏ) ԏ += ы; if (ы == 0 || ԏ > ˋ.Count - 1 || ԏ < 0) ΰ = true;
                    else
                    {
                        Ԋ = ы > 0 ? ˋ.Count - 1 - ԏ : ԏ; B = ˋ[
                    ԏ]; Ә = B; if (ԏ >= 1 && ԏ < ˋ.Count - 1) Ԇ = B.ɉ - ˋ[ԏ - ы].ɉ; else Ә = null; ĉ = B.ɉ; ӥ = true;
                    }
                }
                if (Ӧ) ë(Ӽ.Ӻ.ç, Ӽ.Ӻ.Ï, Ӽ.Ӻ.ĕ, false);
                else if (ӣ) è(Ӽ.Ӻ.ç,
                    10, true);
                else if (ӥ && B != null) if (ы > 0) ë(B.ç, B.Ï, B.ĕ, 90, false); else ë(B.ç, -B.Ï, -B.ĕ, 90, false); ñ(true, false, true, ĉ, Ԇ, Ә == null ? 0 :
                    Ә.Ί, Ӥ); if (ΰ) { Ԋ = 0; if (Ҡ == ҍ.ү || ƽ == В.Ͼ) { Ҝ(ҍ.Ҭ); return; } if (Ҡ == ҍ.Ү && ԋ) { Ҝ(ҍ.ҭ); return; } if (Ҡ == ҍ.Ү) { Ҝ(ҍ.ʟ); return; } }
            }
            if (Ҡ == ҍ.Ҭ || Ҡ
                    == ҍ.ҩ)
            {
                ͷ ҋ = ˡ(); if (Ԏ) { if (!ˠ(ҋ, dockDist * Ю, true, out ĉ)) { Ͻ(Ѓ.Ё); Ҕ(); return; } ñ(ĉ, 0); è(ҋ.ç, 90, true); }
                if (Ԍ < followPathDock * Ю && Ԍ != -
                    1) { ñ(ĉ, 10); ë(ҋ.ç, ҋ.Ï, ҋ.ĕ, false); }
                if (Ò(MyShipConnectorStatus.Connectable) != null || Ò(MyShipConnectorStatus.Connected) != null)
                { Ҝ(ҍ.ґ); return; }
                if (Ԍ < wpReachedDist / 2 && Ԍ != -1) { Ҝ(ҍ.ҫ); return; }
            }
            if (Ҡ == ҍ.ҫ || Ҡ == ҍ.Ҫ)
            {
                if (Ԏ)
                {
                    if (Ҡ == ҍ.ҫ)
                    {
                        ͷ ҋ = ˡ(); if (!ˠ(ҋ, dockDist
                * Ю, true, out ĉ)) { Ͻ(Ѓ.Ё); Ҕ(); return; }
                        ñ(true, true, false, ĉ, 0); ë(ҋ.ç, ҋ.Ï, ҋ.ĕ, 10, false);
                    }
                    if (Ҡ == ҍ.Ҫ)
                    {
                        ë(Ǟ.ç, Ǟ.Ï, Ǟ.ĕ, 0.5f, false); ĉ
                = Ǟ.ɉ; ñ(true, true, false, ĉ, 0);
                    }
                }
                if (ě) { á(false, 0, 0, 0, 0); if (Ҡ == ҍ.ҫ) Ҝ(ҍ.ґ); if (Ҡ == ҍ.Ҫ) Ҕ(); return; }
            }
            if (Ҡ == ҍ.ґ)
            {
                if (Ò(
                MyShipConnectorStatus.Connected) != null) { if (ƽ == В.Ͼ) Ҝ(ҍ.ҡ); else Ҝ(ҍ.Ҩ); return; }
                ͷ ҋ = ˡ(); if (Ԏ) { Ԅ = 0; ż = DateTime.Now; ԉ = 0; ë(ҋ.ç, ҋ.Ï, ҋ.ĕ, false); }
                Vector3I ѩ = new Vector3I((int)ҋ.Ή.X, (int)ҋ.Ή.Y, (int)ҋ.Ή.Z); IMySlimBlock ʣ = Me.CubeGrid.GetCubeBlock(ѩ); float к = dockingSpeed; float
                й = dockingSpeed * 5; float и = Math.Max(1.5f, Math.Min(5f, Ю * 0.15f)); if (!ˠ(ҋ, 0, true, out ĉ) || !ˠ(ҋ, и, true, out Ԇ) || ʣ == null || !ʣ.
                FatBlock.IsFunctional) { Ͻ(Ѓ.Ё); Ҕ(); return; }
                if (Ԅ == 1 || (Vector3.Distance(ɉ, ĉ) <= и * 1.1f && !Ԏ)) Ԅ = 1;
                else
                {
                    Vector3 з = Ȑ(Э, Ԇ - ɉ); Vector3 ж = Ȑ(Э
                , Э.GetNaturalGravity()); float Ƶ = ē(з, ж, null); к = Math.Min(й, Ƶ);
                }
                ñ(true, false, false, ĉ, ĉ - ɉ, dockingSpeed, к); if (Ԏ) ԅ = (float)Ԍ;
                IMyShipConnector Ä = Ò(MyShipConnectorStatus.Connectable); if (Ä != null)
                {
                    ñ(false, false, false, ĉ, 0); if (ԉ > 0) ԉ = 0; ԉ--; if (ԉ < -5)
                    {
                        Ä.Connect(); if (Ä.
                Status == MyShipConnectorStatus.Connected) { if (ƽ == В.Ͼ) Ҝ(ҍ.ҡ); else Ҝ(ҍ.Ҩ); å(); ť(Ч, true); return; }
                    }
                }
                else
                {
                    float ț = (float)Math.Round(Ԍ
                , 1); if (ț < ԅ) { ԉ = -1; ԅ = ț; } else ԉ++; if (ԉ > 20) { Ҝ(ҍ.ҩ); return; }
                }
            }
            if (Ҡ == ҍ.Ҩ || Ҡ == ҍ.ҡ || Ҡ == ҍ.Ĺ || Ҡ == ҍ.ļ || Ҡ == ҍ.Ŧ)
            {
                bool л = false; bool е =
                false; if (ƽ == В.Ͼ) { if (ˡ() == Β) л = true; else if (ˡ() == Ǟ) е = true; }
                if (Ԏ)
                {
                    Ɔ = false; if (Ò(MyShipConnectorStatus.Connected) == null)
                    {
                        Ҝ(ҍ.ҧ);
                        return;
                    }
                    å(); if (л) ʘ(ʔ.Ɣ); if (е) ʘ(ʒ.Ɣ); ӛ = false; ә = false; Ӛ = false; ӧ = false;
                }
                if (Ò(MyShipConnectorStatus.Connected) == null)
                {
                    Ҕ(); Ͻ(Ѓ.Ͽ);
                    return;
                }
                if (Ӄ != ӌ.Ӊ || ɸ == -1 || ĥ == И.ɗ) ә = true; else if (Ĩ >= 100f) ә = true; else if (Ĩ <= 99f) ә = false; if (Ӄ != ӌ.Ӊ || ɶ == -1 || Ф.Count == 0) Ӛ = true;
                else
                    if (ļ >= 100f) Ӛ = true; else if (ļ <= 99) Ӛ = false; if (Ӄ != ӌ.Ӊ || ɷ == -1 || Ц.Count == 0) ӧ = true; else ӧ = Ĺ >= ɷ; ų ƌ = null; if (л) ƌ = ʔ; if (е) ƌ = ʒ; if (ƌ !=
                    null && (ƌ.Ų == ſ.Ÿ || ƌ.Ų == ſ.ŷ)) ә = true; if (ƌ != null && (ƌ.Ų == ſ.Ź)) if (!ӛ) ә = false; if (ƌ != null && (ƌ.Ų == ſ.ŵ || ƌ.Ų == ſ.Ŵ)) Ӛ = true; if (ƌ != null && (
                    ƌ.Ų == ſ.Ŷ)) if (!ӛ) Ӛ = false; if (ο)
                {
                    ChargeMode д = ә ? ChargeMode.Auto : ChargeMode.Recharge; if (ƌ != null && (ƌ.Ų == ſ.ŷ || ƌ.Ų == ſ.Ÿ)) д =
                    ChargeMode.Discharge; ŧ(д); Ŕ(!Ӛ);
                }
                if (!ӛ) { if (ƽ == В.Ͼ) ӛ = Ӄ != ӌ.Ӊ || Ɖ(Ԏ, true) || Μ; else ӛ = Ӄ != ӌ.Ӊ || ş(); }
                else
                {
                    if (!ә) Ҝ(ҍ.Ŧ); if (!Ӛ) Ҝ(ҍ.ļ); if (!ӧ)
                        Ҝ(ҍ.Ĺ); Ԏ = false;
                }
                if (ӛ && ә && Ӛ && ӧ)
                {
                    ŧ(ChargeMode.Auto); Ŕ(false); if (Ӄ == ӌ.Ӊ)
                    {
                        if (ƽ == В.Ͼ)
                        {
                            if (ˡ() == Β) ʘ(ʔ.ƕ); else if (ˡ() == Ǟ) ʘ(ʒ.ƕ);
                            if (ˡ() == Β) ˣ(Ǟ, ӌ.ӆ); else ˣ(Β, ӌ.Ӈ);
                        }
                    }
                    Ҝ(ҍ.ҧ); return;
                }
            }
            if (Ҡ == ҍ.ҧ)
            {
                if (Ԏ)
                {
                    IMyShipConnector Ä = Ò(MyShipConnectorStatus.Connected);
                    if (Ä == null) { Ҝ(ҍ.Ү); return; }
                    IMyShipConnector г = Ä.OtherConnector; Ŗ(Ä, false); ť(Ч, false); ͷ B = null; if (Vector3.Distance(Ä.
                    GetPosition(), Β.ɉ) < 5f && Β.Ͷ) B = Β; if (Vector3.Distance(Ä.GetPosition(), Ǟ.ɉ) < 5f && Ǟ.Ͷ) B = Ǟ; if (B != null)
                    {
                        if (!ˠ(B, dockDist * Ю, true, out ĉ))
                        {
                            Ͻ(Ѓ
                    .Ё); Ҕ(); return;
                        }
                        ñ(ĉ, 5); ë(B.ç, B.Ï, B.ĕ, false);
                    }
                    else ñ(ɉ + г.WorldMatrix.Forward * dockDist * Ю, 5); if (Ӄ == ӌ.Ӊ) Ͻ(Ѓ.Ђ);
                }
                if (Ԍ <
                    wpReachedDist) { Ŗ(Ъ, true); Ҝ(ҍ.Ү); return; }
            }
            if (Ҡ == ҍ.ҭ)
            {
                if (Ԏ) { Ũ(true); Ŗ(Ш, false); ĉ = Ǟ.ɉ; ñ(ĉ, 20); ë(Ǟ.ç, Ǟ.Ï, Ǟ.ĕ, false); }
                if (Ԍ < wpReachedDist / 2
                    ) { Ҝ(ҍ.Ҫ); return; }
            }
            Ԏ = false;
        }
        class в { public в(Vector3 б, float Ʌ) { this.б = б; this.Ʌ = Ʌ; } public Vector3 б; public float Ʌ; }
        void н
                    (List<ͷ> ˋ, int ы, List<Vector3> ъ, float Ʌ, bool Ç, ref int Ћ)
        {
            if (Ç) { for (int ã = 0; ã < ˋ.Count; ã++) ˋ[ã].Ί = 0; Ћ = -1; return; }
            if (ы == 0)
                return; int щ = ы * -1; if (Ћ == -1) Ћ = щ > 0 ? 1 : ˋ.Count - 2; int Ř = 0; while (Ћ >= 1 && Ћ < ˋ.Count - 1)
            {
                if (Ř > 50) return; Ř++; try
                {
                    if ((щ < 0 && Ћ >= 1) || (щ > 0 && Ћ <=
                ˋ.Count - 2))
                    {
                        ͷ ʝ = ˋ[Ћ]; bool ш = false; for (int ʡ = 0; ʡ < ъ.Count; ʡ++) { if (Vector3.Distance(ʝ.ɉ, ъ[ʡ]) <= Ʌ) { ш = true; break; } }
                        if (!ш)
                        {
                            ͷ ч =
                ˋ[Ћ - щ]; ͷ ц = ˋ[Ћ + щ]; Vector3 х = ʝ.ɉ - ц.ɉ; Vector3 ф = ч.ɉ - ʝ.ɉ; Vector3 у = ʝ.ɉ + Vector3.Normalize(х) * ф.Length(); Vector3 т = ч.ɉ - у;
                            Vector3 с = ȕ(ы > 0 ? ʝ.Ï : ʝ.Ï * -1, ʝ.ç * -1, т); Vector3 р = ȕ(ы > 0 ? ʝ.Ï : ʝ.Ï * -1, ʝ.ç * -1, ф); Vector3 п = ȕ(ы > 0 ? ʝ.Ï : ʝ.Ï * -1, ʝ.ç * -1, ʝ.Í); ʝ.Ί = (float)
                            Math.Sqrt(Math.Pow(ч.Ί, 2) + Math.Pow(ē(-р, п, ʝ), 2)); for (int ʡ = 0; ʡ < ъ.Count; ʡ++) if (Vector3.Distance(ч.ɉ, ъ[ʡ]) <= Ʌ)
                                {
                                    Vector3 о = ȕ(ы > 0
                            ? ʝ.Ï : ʝ.Ï * -1, ʝ.ç * -1, ъ[ʡ] - ʝ.ɉ); float ь = ē(-о, п, ʝ); ʝ.Ί = Math.Min(ʝ.Ί, ь) / 2f;
                                }
                            if (с.Length() == 0) с = new Vector3(0, 0, 1); Vector3 а = ȕ(
                            ʝ.Ï, ʝ.ç * -1, ʝ.Í); float â = Â(с, а, ʝ); float ª = µ(â, ϋ); float K = (float)Math.Sqrt(с.Length() * 1.0f / (0.5f * ª)); ʝ.Ί = Math.Min(ʝ.Ί, (ф.
                            Length() / K) * ɵ);
                        }
                    }
                }
                catch { return; }
                Ћ += щ;
            }
            Ћ = -1;
        }
        void З(bool Ç)
        {
            if (Ç) { ӎ = 0; return; }
            if (ƽ == В.Ͼ) return; float ʝ = ԑ * Math.Max(1, ʁ); if (Ԑ == ԑ)
                ʝ += Math.Min(ʁ, ԁ); float ņ = Ӂ * Ӏ * Math.Max(1, ʁ); ӎ = Math.Max(ӎ, (float)Math.Min(ʝ / ņ * 100.0, 100));
        }
        MyDetectedEntityType Ж()
        {
            try
            {
                if (
                Ƃ(Ь, true) && !Ь.LastDetectedEntity.IsEmpty()) return Ь.LastDetectedEntity.Type;
            }
            catch { }; return MyDetectedEntityType.None;
        }
        float Е(bool Ï) { if (ƽ == В.ψ && Ж() == MyDetectedEntityType.None && !ƃ(Ь, true)) return fastSpeed; else return Ï ? ɴ : ɳ; }
        public enum И
        {
            ɗ, Ŧ, Д
        , Г
        }
        public enum В { ǻ, Б, ψ, А, Ͼ }
        В ƽ = В.ǻ; int ɘ = 0; float Й = 0; float Я = 0; float Ю = 0; IMyRemoteControl Э; IMySensorBlock Ь; List<
        IMyTimerBlock> Ы = new List<IMyTimerBlock>(); List<IMyShipConnector> Ъ = new List<IMyShipConnector>(); List<IMyThrust> Δ = new List<IMyThrust>()
        ; List<IMyGyro> Щ = new List<IMyGyro>(); List<IMyTerminalBlock> Ш = new List<IMyTerminalBlock>(); List<IMyLandingGear> Ч = new List<
        IMyLandingGear>(); List<IMyReactor> Ц = new List<IMyReactor>(); List<IMyConveyorSorter> Х = new List<IMyConveyorSorter>(); List<IMyGasTank> Ф =
        new List<IMyGasTank>(); List<IMyTerminalBlock> У = new List<IMyTerminalBlock>(); List<IMyTerminalBlock> Т = new List<
        IMyTerminalBlock>(); List<IMyTerminalBlock> С = new List<IMyTerminalBlock>(); List<IMyBatteryBlock> Р = new List<IMyBatteryBlock>(); List<
        IMyTextPanel> П = new List<IMyTextPanel>(); List<IMyTextSurface> О = new List<IMyTextSurface>(); List<IMyTextPanel> Н = new List<IMyTextPanel>(
        ); IMyTerminalBlock М = null; bool Л(IMyTerminalBlock q) => q.CubeGrid == Me.CubeGrid; void К() { χ.GetBlocksOfType(У, Л); }
        void э()
        {
            Н
        .Clear(); for (int A = П.Count - 1; A >= 0; A--)
            {
                String Ǩ = П[A].CustomData.ToUpper(); bool Ѷ = false; if (Ǩ == ȣ) { Ѷ = true; Ν = true; }
                if (Ǩ == Ȣ) Ѷ =
        true; if (Ѷ) { Н.Add(П[A]); П.RemoveAt(A); }
            }
            Ţ(Н, false, 1, false);
        }
        void ѵ(List<IMyTerminalBlock> Ī)
        {
            О.Clear(); for (int A = 0; A < Ī.Count; A
        ++)
            {
                IMyTerminalBlock q = Ī[A]; try
                {
                    String љ = pamTag.Substring(0, pamTag.Length - 1) + ":"; int E = q.CustomName.IndexOf(љ); int Ѵ = -1; if
        (E < 0 || !int.TryParse(q.CustomName.Substring(E + љ.Length, 1), out Ѵ)) continue; if (Ѵ == -1) continue; Ѵ--; IMyTextSurfaceProvider ѳ = (
        IMyTextSurfaceProvider)q; if (Ѵ < ѳ.SurfaceCount && Ѵ >= 0) { О.Add(ѳ.GetSurface(Ѵ)); }
                }
                catch { }
            }
        }
        void Ѳ()
        {
            if (Э == null) return; М = null; float ѱ = 0, Ѱ = 0, ѯ = 0, Ѯ = 0,
        ѭ = 0, Ѭ = 0; List<IMyTerminalBlock> ѫ = Ѧ(Ш, pamTag, true); bool Ѫ = ѫ.Count == 0; if (ѫ.Count > 0) М = ѫ[0]; else if (Ш.Count > 0) М = Ш[0]; int Ř = 0;
            for (int A = 0; A < Ш.Count; A++)
            {
                if (Ш[A].WorldMatrix.Forward != М.WorldMatrix.Forward)
                {
                    if (Ѫ)
                    {
                        ɘ = 2; ώ = "Mining direction is unclear!";
                        return;
                    }
                    continue;
                }
                Ř++; Vector3 ѷ = Ȓ(Э, Ш[A].GetPosition()); if (A == 0) { ѱ = ѷ.X; Ѱ = ѷ.X; ѯ = ѷ.Y; Ѯ = ѷ.Y; ѭ = ѷ.Z; Ѭ = ѷ.Z; }
                Ѱ = Math.Max(ѷ.X, Ѱ); ѱ = Math
                        .Min(ѷ.X, ѱ); Ѯ = Math.Max(ѷ.Y, Ѯ); ѯ = Math.Min(ѷ.Y, ѯ); Ѭ = Math.Max(ѷ.Z, Ѭ); ѭ = Math.Min(ѷ.Z, ѭ);
            }
            Й = (Ѱ - ѱ) * (1 - ɼ) + drillRadius * 2; Я = (Ѯ - ѯ) *
                        (1 - ɲ) + drillRadius * 2; if (М != null && М.WorldMatrix.Forward == Э.WorldMatrix.Down) Я = (Ѭ - ѭ) * (1 - ɲ) + drillRadius * 2;
        }
        void Ѹ()
        {
            if (ɜ)
            {
                ɘ = 2
                        ; return;
            }
            List<IMyRemoteControl> ҁ = new List<IMyRemoteControl>(); List<IMySensorBlock> Ҋ = new List<IMySensorBlock>(); List<
                        IMyTerminalBlock> ƣ = new List<IMyTerminalBlock>(); χ.GetBlocksOfType(ҁ, Л); χ.GetBlocksOfType(П, Л); χ.GetBlocksOfType(Ҋ, Л); χ.
                        SearchBlocksOfName(pamTag.Substring(0, pamTag.Length - 1) + ":", ƣ, q => q.CubeGrid == Me.CubeGrid && q is IMyTextSurfaceProvider); П = Ѧ(П, pamTag, true); э
                        (); ѵ(ƣ); Ţ(П, setLCDFontAndSize, 1.4f, false); Ţ(О, setLCDFontAndSize, 1.4f, true); List<IMySensorBlock> O = Ѧ(Ҋ, pamTag, true); if (O.
                        Count > 0) Ь = O[0];
            else Ь = null; if (ƽ == В.Б)
            {
                χ.GetBlocksOfType(Ш, q => q.CubeGrid == Me.CubeGrid && q is IMyShipDrill); if (Ш.Count == 0)
                {
                    ɘ = 1; ώ
                        = "Drills are missing";
                }
            }
            else if (ƽ == В.ψ)
            {
                χ.GetBlocksOfType(Ш, q => q.CubeGrid == Me.CubeGrid && q is IMyShipGrinder); if (Ш.Count ==
                        0) { ɘ = 1; ώ = "Grinders are missing"; }
                if (ƽ == В.ψ && Ь == null) { ɘ = 1; ώ = "Sensor is missing"; }
            }
            else if (ƽ == В.Ͼ)
            {
                χ.GetBlocksOfType(Ы, q => q
                        .CubeGrid == Me.CubeGrid);
            }
            List<IMyRemoteControl> Ķ = Ѧ(ҁ, pamTag, true); if (Ķ.Count > 0) ҁ = Ķ; if (ҁ.Count > 0) Э = ҁ[0];
            else
            {
                Э = null; ɘ = 2; ώ =
                        "Remote is missing"; return;
            }
            Ю = (float)Э.CubeGrid.WorldVolume.Radius * 2; М = null; if (ƽ != В.Ͼ)
            {
                Ѳ(); if (Ш.Count > 0 && М != null)
                {
                    if (Ь != null && (М.
                        WorldMatrix.Forward != Ь.WorldMatrix.Forward || !(Э.WorldMatrix.Forward == Ь.WorldMatrix.Up || Э.WorldMatrix.Down == Ь.WorldMatrix.Down)))
                    {
                        ɘ =
                        1; ώ = "Wrong sensor direction";
                    }
                    if (М.WorldMatrix.Forward != Э.WorldMatrix.Forward && М.WorldMatrix.Forward != Э.WorldMatrix.Down)
                    { ɘ = 2; ώ = "Wrong remote direction"; }
                }
            }
        }
        void Ҁ()
        {
            χ.GetBlocksOfType(Ч, Л); χ.GetBlocksOfType(Ъ, Л); χ.GetBlocksOfType(Δ, Л); χ.
                    GetBlocksOfType(Щ, Л); χ.GetBlocksOfType(Р, Л); χ.GetBlocksOfType(Ц, Л); χ.GetBlocksOfType(Ф, q => q.CubeGrid == Me.CubeGrid && q.BlockDefinition.
                    ToString().ToUpper().Contains("HYDROGEN")); χ.GetBlocksOfType(Х, Л); if (Me.CubeGrid.GridSizeEnum == MyCubeSize.Small) Ъ = Ѧ(Ъ,
                    "ConnectorMedium", false);
            else Ъ = Ѧ(Ъ, "Connector", false); List<IMyShipConnector> ѿ = Ѧ(Ъ, pamTag, true); if (ѿ.Count > 0) Ъ = ѿ; if (ɘ <= 1)
            {
                if (Ъ.Count == 0)
                {
                    ɘ = 1; ώ = "Connector is missing";
                }
                if (Щ.Count == 0) { ɘ = 1; ώ = "Gyros are missing"; }
                if (Δ.Count == 0) { ɘ = 1; ώ = "Thrusters are missing"; }
            }
            List<IMyConveyorSorter> Ѿ = Ѧ(Х, pamTag, true); if (Ѿ.Count > 0) Х = Ѿ; List<IMyLandingGear> ѽ = Ѧ(Ч, pamTag, true); if (ѽ.Count > 0) Ч = ѽ; for (int A
            = 0; A < Ч.Count; A++) Ч[A].AutoLock = false; List<IMyBatteryBlock> Ѽ = Ѧ(Р, pamTag, true); if (Ѽ.Count > 0) Р = Ѽ; List<IMyGasTank> ѻ = Ѧ(Ф,
            pamTag, true); if (ѻ.Count > 0) Ф = ѻ;
        }
        void Ѻ()
        {
            χ.GetBlocksOfType(С, q => q.CubeGrid == Me.CubeGrid && q.InventoryCount > 0); Т.Clear(); for (int
            A = С.Count - 1; A >= 0; A--) { if (Ѩ(С[A])) { Т.Add(С[A]); С.RemoveAt(A); } }
        }
        bool ѹ(IMyTerminalBlock q)
        {
            if (q.InventoryCount == 0) return
            false; if (ƽ == В.Ͼ) return true; for (int ã = 0; ã < Ш.Count; ã++)
            {
                IMyTerminalBlock K = Ш[ã]; if (K == null || !Ƃ(K, true) || K.InventoryCount == 0)
                    continue; if (!checkConveyorSystem || K.GetInventory(0).IsConnectedTo(q.GetInventory(0))) { return true; }
            }
            return false;
        }
        bool Ѩ(
                    IMyTerminalBlock q)
        {
            if (q is IMyCargoContainer) return true; if (q is IMyShipDrill) return true; if (q is IMyShipGrinder) return true; if (q is
                    IMyShipConnector)
            {
                if (((IMyShipConnector)q).ThrowOut) return false; if (Me.CubeGrid.GridSizeEnum != MyCubeSize.Large && ї(q, "ConnectorSmall",
                    false)) return false;
                else return true;
            }
            return false;
        }
        List<ŕ> Ѧ<ŕ>(List<ŕ> Ī, String ј, bool і)
        {
            List<ŕ> J = new List<ŕ>(); for (int A = 0;
                    A < Ī.Count; A++) if (ї(Ī[A], ј, і)) J.Add(Ī[A]); return J;
        }
        bool ї<ŕ>(ŕ Ō, String љ, bool і)
        {
            IMyTerminalBlock q = (IMyTerminalBlock)Ō;
            if (і && q.CustomName.ToUpper().Contains(љ.ToUpper())) return true; if (!і && q.BlockDefinition.ToString().ToUpper().Contains(љ.
            ToUpper())) return true; return false;
        }
        Dictionary<String, float> ѕ = new Dictionary<String, float>(); int є = 0; void ѓ()
        {
            if (!ɾ) return; if (
            Ш.Count <= 1) return; float ђ = 0; float ё = 0; for (int A = 0; A < Ш.Count; A++)
            {
                IMyTerminalBlock ѐ = Ш[A]; if (ƃ(ѐ, true)) continue; ђ += (float)
            ѐ.GetInventory(0).MaxVolume; ё += (float)ѐ.GetInventory(0).CurrentVolume;
            }
            float я = (float)Math.Round(µ(ё, ђ), 5); for (int A = 0; A <
            Math.Max(1, Math.Floor(Ш.Count / 10f)); A++)
            {
                float ю = 0; float њ = 0; float ѧ = 0; float ѥ = 0; IMyTerminalBlock ĝ = null; IMyTerminalBlock ȵ =
            null; for (int ã = 0; ã < Ш.Count; ã++)
                {
                    IMyTerminalBlock ѐ = Ш[ã]; if (ƃ(ѐ, true)) continue; float Ѥ = (float)ѐ.GetInventory(0).MaxVolume;
                    float ѣ = µ((float)ѐ.GetInventory(0).CurrentVolume, Ѥ); if (ĝ == null || ѣ > ю) { ĝ = ѐ; ю = ѣ; ѧ = Ѥ; }
                    if (ȵ == null || ѣ < њ) { ȵ = ѐ; њ = ѣ; ѥ = Ѥ; }
                }
                if (ĝ == null ||
                    ȵ == null || ĝ == ȵ) return; if (checkConveyorSystem && !ĝ.GetInventory(0).IsConnectedTo(ȵ.GetInventory(0)))
                {
                    if (є > 20) ώ =
                    "Inventory balancing failed";
                    else є++; return;
                }
                є = 0; List<MyInventoryItem> Ŏ = new List<MyInventoryItem>(); ĝ.GetInventory(0).GetItems(Ŏ); float ѝ = 0; if (Ŏ.
                    Count == 0) continue; MyInventoryItem Ĵ = Ŏ[0]; String Ѣ = Ĵ.Type.TypeId + Ĵ.Type.SubtypeId; if (!ѕ.TryGetValue(Ѣ, out ѝ))
                {
                    if (џ(ĝ.
                    GetInventory(0), 0, ȵ.GetInventory(0), out ѝ)) { ѕ.Add(Ѣ, ѝ); }
                    else { return; }
                }
                float ѡ = ((ю - я) * ѧ / ѝ); float Ѡ = ((я - њ) * ѥ / ѝ); int Ń = (int)Math.Min(Ѡ,
                    ѡ); if (Ń <= 0) return; if ((float)Ĵ.Amount < Ń) ĝ.GetInventory(0).TransferItemTo(ȵ.GetInventory(0), 0, null, null, Ĵ.Amount);
                else ĝ.
                    GetInventory(0).TransferItemTo(ȵ.GetInventory(0), 0, null, null, Ń);
            }
        }
        bool џ(IMyInventory Ǡ, int E, IMyInventory ў, out float ѝ)
        {
            ѝ = 0; float
                    ќ = (float)Ǡ.CurrentVolume; List<MyInventoryItem> ћ = new List<MyInventoryItem>(); Ǡ.GetItems(ћ); float ƻ = 0; for (int A = 0; A < ћ.Count
                    ; A++) ƻ += (float)ћ[A].Amount; Ǡ.TransferItemTo(ў, E, null, null, 1); float ɱ = ќ - (float)Ǡ.CurrentVolume; ћ.Clear(); Ǡ.GetItems(ћ);
            float ò = 0; for (int A = 0; A < ћ.Count; A++) ò += (float)ћ[A].Amount; if (ɱ == 0f || !Ȧ(0.9999, ƻ - ò, 1.0001)) { return false; }
            ѝ = ɱ; return true;
        }
        float ō(IMyTerminalBlock Ō, String Ô, String ń, String[] ŋ)
        {
            float J = 0; for (int ã = 0; ã < Ō.InventoryCount; ã++)
            {
                IMyInventory Ŋ = Ō.
        GetInventory(ã); List<MyInventoryItem> Ŏ = new List<MyInventoryItem>(); Ŋ.GetItems(Ŏ); foreach (MyInventoryItem Ĵ in Ŏ)
                {
                    if (ŋ != null && (ŋ.
        Contains(Ĵ.Type.TypeId.ToUpper()) || ŋ.Contains(Ĵ.Type.SubtypeId.ToUpper()))) continue; if ((Ô == "" || Ĵ.Type.TypeId.ToUpper() == Ô) && (ń ==
        "" || Ĵ.Type.SubtypeId.ToUpper() == ń)) J += (float)Ĵ.Amount;
                }
            }
            return J;
        }
        public enum ŉ { ň, Ň, ņ }
        class Ņ
        {
            public String Ô = ""; public
        String ń = ""; public int Ń = 0; public ŉ ĭ = ŉ.ņ; public Ņ(String Ô, String ń, int Ń, ŉ ĭ) { this.Ô = Ô; this.ń = ń; this.Ń = Ń; this.ĭ = ĭ; }
        }
        Ņ Œ(
        String Ô, String ń, ŉ ĭ, bool ő)
        {
            Ô = Ô.ToUpper(); ń = ń.ToUpper(); for (int A = 0; A < ŀ.Count; A++)
            {
                Ņ Ĵ = ŀ[A]; if (Ĵ.Ô.ToUpper() == Ô && Ĵ.ń.ToUpper
        () == ń && (Ĵ.ĭ == ĭ || ĭ == ŉ.ņ)) return Ĵ;
            }
            Ņ J = null; if (ő) { J = new Ņ(Ô, ń, 0, ĭ); ŀ.Add(J); }
            return J;
        }
        int Ő(String Ô, String ń, ŉ ĭ)
        {
            return
        Ő(Ô, ń, ĭ, null);
        }
        int Ő(String Ô, String ń, ŉ ĭ, String[] ŋ)
        {
            int Ń = 0; Ô = Ô.ToUpper(); ń = ń.ToUpper(); ; for (int A = 0; A < ŀ.Count; A++)
            {
                Ņ Ĵ
        = ŀ[A]; if (ŋ != null && ŋ.Contains(Ĵ.Ô.ToUpper())) continue; if ((Ô == "" || Ĵ.Ô.ToUpper() == Ô) && (ń == "" || Ĵ.ń.ToUpper() == ń) && (Ĵ.ĭ == ĭ || ĭ
        == ŉ.ņ)) Ń += Ĵ.Ń;
            }
            return Ń;
        }
        float ŏ = 0; float œ = 0; float ł = 0; List<Ņ> ŀ = new List<Ņ>(); void Į(IMyTerminalBlock q, ŉ ĭ)
        {
            for (int A = 0; A
        < q.InventoryCount; A++)
            {
                List<MyInventoryItem> Ĭ = new List<MyInventoryItem>(); q.GetInventory(A).GetItems(Ĭ); for (int ã = 0; ã < Ĭ.
        Count; ã++) { Œ(Ĭ[ã].Type.SubtypeId, Ĭ[ã].Type.TypeId.Replace("MyObjectBuilder_", ""), ĭ, true).Ń += (int)Ĭ[ã].Amount; }
            }
        }
        void ī(List<Ņ
        > Ī)
        { for (int ĩ = Ī.Count - 1; ĩ > 0; ĩ--) { for (int A = 0; A < ĩ; A++) { Ņ ª = Ī[A]; Ņ q = Ī[A + 1]; if (ª.Ń < q.Ń) Ī.Move(A, A + 1); } } }
        void ħ()
        {
            try
            {
                ŀ.
        Clear(); for (int A = 0; A < Т.Count; A++) { IMyTerminalBlock q = Т[A]; if (!Ƃ(q, true)) continue; Į(q, ŉ.ň); }
                if (ɻ != ʪ.ɗ)
                {
                    for (int A = 0; A < С.Count;
        A++) { IMyTerminalBlock q = С[A]; if (!Ƃ(q, true)) continue; Į(q, ŉ.Ň); }
                }
                ī(ŀ);
            }
            catch (Exception e) { ȥ = e; }
        }
        void Ħ()
        {
            ł = 0; œ = 0; try
            {
                for (
        int A = 0; A < Т.Count; A++)
                {
                    IMyTerminalBlock q = Т[A]; if (!Ƃ(q, true)) continue; œ += (float)q.GetInventory(0).CurrentVolume; ł += (float)q
        .GetInventory(0).MaxVolume;
                }
                ŏ = (float)Math.Min(Math.Round(µ(œ, ł) * 100, 1), 100.0);
            }
            catch (Exception e) { ȥ = e; }
        }
        float Ĩ = 0; И ĥ;
        void İ()
        {
            float Ł = 0, Ŀ = 0, ľ = 0, Ľ = 0; for (int A = 0; A < Р.Count; A++)
            {
                IMyBatteryBlock q = Р[A]; if (!Ƃ(q, true)) continue; Ł += q.MaxStoredPower;
                Ŀ += q.CurrentStoredPower; ľ += q.CurrentInput; Ľ += q.CurrentOutput;
            }
            Ĩ = (float)Math.Round(µ(Ŀ, Ł) * 100, 1); if (ľ >= Ľ) ĥ = И.Ŧ; else ĥ = И.Г;
            if (ľ == 0 && Ľ == 0 || Ĩ == 100.0) ĥ = И.Д; if (Р.Count == 0) ĥ = И.ɗ;
        }
        float ļ = 0; void Ļ()
        {
            float ĺ = 0; for (int A = 0; A < Ф.Count; A++)
            {
                IMyGasTank q = Ф[
            A]; if (!Ƃ(q, true)) continue; ĺ += (float)q.FilledRatio;
            }
            ļ = µ(ĺ, Ф.Count) * 100f;
        }
        float Ĺ = 0; String ĸ = ""; void ķ()
        {
            Ĺ = 0; try
            {
                for (int A =
            0; A < Ц.Count; A++)
                {
                    IMyReactor Ķ = Ц[A]; if (!Ƃ(Ķ, true)) continue; List<MyInventoryItem> Ī = new List<MyInventoryItem>(); Ķ.
            GetInventory(0).GetItems(Ī); float ĵ = 0; for (int ã = 0; ã < Ī.Count; ã++)
                    {
                        MyInventoryItem Ĵ = Ī[ã]; if (Ĵ.Type.SubtypeId.ToUpper() == "URANIUM" && Ĵ.
            Type.TypeId.ToUpper().Contains("_INGOT")) ĵ += (float)Ĵ.Amount;
                    }
                    if (ĵ < Ĺ || A == 0) { Ĺ = ĵ; ĸ = Ķ.CustomName; }
                }
            }
            catch (Exception e) { ȥ = e; }
        }
        void ĳ() { if (Ҳ) { if (į().Count > Җ) { Ҳ = false; if (ʆ != ʯ.ʫ) { Ҕ(); if (ʆ == ʯ.ʭ) Ґ(); if (ʆ == ʯ.ʮ) if (Β.Ͷ) ҏ(); else Ґ(); } ώ = "Damage detected"; } } }
        bool Ĳ()
        {
            if (!λ) return true; if (Ӄ == ӌ.Ӊ)
            {
                if (ɸ > 0 && ĥ != И.ɗ) { if (Ĩ <= ɸ) { ώ = "Low energy! Move home"; return false; } }
                if (ɷ > 0 && Ц.Count > 0)
                {
                    if (Ĺ <= ɷ) { ώ = "Low fuel: " + ĸ; return false; }
                }
                if (ɶ > 0 && Ф.Count > 0) { if (ļ <= ɶ) { ώ = "Low hydrogen"; return false; } }
            }
            return true;
        }
        List<
                    IMyTerminalBlock> į()
        {
            List<IMyTerminalBlock> ı = new List<IMyTerminalBlock>(); for (int A = 0; A < У.Count; A++)
            {
                IMyTerminalBlock q = У[A]; if (ƃ(q,
                    false)) ı.Add(q);
            }
            return ı;
        }
        bool ƃ(IMyTerminalBlock Ō, bool Ɓ) { return (!Ƃ(Ō, Ɓ) || !Ō.IsFunctional); }
        bool Ƃ(IMyTerminalBlock q, bool
                    Ɓ)
        {
            if (q == null) return false; try
            {
                IMyCubeBlock ƀ = Me.CubeGrid.GetCubeBlock(q.Position).FatBlock; if (Ɓ) return ƀ == q;
                else return
                    ƀ.GetType() == q.GetType();
            }
            catch { return false; }
        }
        public enum ſ { ž, Ž, ż, Ż, ź, Ź, Ÿ, ŷ, Ŷ, ŵ, Ŵ }
        class ų
        {
            public ſ Ų = ſ.ž; public float Ƅ =
                    0; public float ƅ = 0; public string Ɣ = ""; public string ƕ = ""; DateTime Ɠ; public bool ƒ = false; private bool Ƒ = false; public bool
                    Ɛ(bool Ç)
            { if (Ç) { ƅ = 0; Ƒ = false; return false; } Ƒ = true; return ƅ > Ƅ; }
            public void Ç() { Ɛ(true); ƒ = false; }
            public void Ə()
            {
                if (Ƒ) if ((
                    DateTime.Now - Ɠ).TotalSeconds > 1) { ƅ++; Ɠ = DateTime.Now; }
            }
            public bool Ǝ()
            {
                switch (Ų) { case ſ.Ž: return true; case ſ.ż: return true; }
                return
                    false;
            }
        }
        bool ƍ(ų ƌ, bool Ç, bool ƈ)
        {
            if (Ç) ƌ.Ç(); ƌ.Ə(); bool J = false; String O = ""; switch (ƌ.Ų)
            {
                case ſ.ž:
                    {
                        O = "Waiting for command"; J =
                    false; break;
                    }
                case ſ.Ż: { O = "Waiting for cargo"; J = Ű(true); break; }
                case ſ.ź: { O = "Unloading"; J = ş(); break; }
                case ſ.ż: { J = true; break; }
                case ſ.Ź: { O = "Charging batteries"; J = Ĩ >= 100f; break; }
                case ſ.Ÿ: { O = "Discharging batteries"; J = Ĩ <= 25f; break; }
                case ſ.ŷ:
                    {
                        O =
                "Discharging batteries"; J = Ĩ <= 0f; break;
                    }
                case ſ.Ŷ: { O = "Filling up hydrogen"; J = ļ >= 100f; break; }
                case ſ.ŵ: { O = "Unloading hydrogen"; J = ļ <= 25f; break; }
                case
                ſ.Ŵ:
                    { O = "Unloading hydrogen"; J = ļ <= 0f; break; }
                case ſ.Ž:
                    {
                        bool Ƌ = ů(); if (!Ƌ) ƌ.ƒ = true; J = ƌ.ƒ && Ƌ; O = "Waiting for passengers"; break;
                    }
            }
            if (!J) ƌ.Ɛ(true); if (J && ƌ.Ǝ()) { J = ƌ.Ɛ(false); O = "Undocking in: " + Ǖ((int)Math.Max(0, ƌ.Ƅ - ƌ.ƅ)); }
            if (ƈ) Ɗ = O; return J;
        }
        String Ɗ =
                    ""; bool Ɖ(bool Ç, bool ƈ)
        {
            IMyShipConnector Ä = Ò(MyShipConnectorStatus.Connected); if (Ä == null) return false; if (Vector3.Distance
                    (Β.ɉ, Ä.GetPosition()) < 5) return ƍ(ʔ, Ç, ƈ); if (Vector3.Distance(Ǟ.ɉ, Ä.GetPosition()) < 5) return ƍ(ʒ, Ç, ƈ); return false;
        }
        float Ƈ =
                    0; bool Ɔ = false; bool Ű(bool š)
        {
            if (ɿ && ƽ != В.Ͼ) if (Ƈ != -1 && ϋ >= Ƈ) { ώ = "Ship too heavy"; return true; }
            if (ŏ >= ɹ || Ɔ)
            {
                Ɔ = false; ώ =
                    "Ship is full"; return true;
            }
            return false;
        }
        bool ů()
        {
            List<IMyCockpit> Ī = new List<IMyCockpit>(); χ.GetBlocksOfType(Ī, q => q.CubeGrid == Me.
                    CubeGrid); for (int A = 0; A < Ī.Count; A++) if (Ī[A].IsUnderControl) return true; return false;
        }
        bool ş()
        {
            String[] ŋ = null; if (!ɺ) ŋ = new string[
                    ] { "ICE" }; if (ƽ == В.Б) return Ő("", "ORE", ŉ.ň, ŋ) == 0; if (ƽ == В.ψ) return Ő("", "COMPONENT", ŉ.ň, ŋ) == 0; else return Ő("", "", ŉ.ň, ŋ) == 0;
        }
        void Ş(bool ŝ, bool Ŝ, float ś, float Š)
        {
            if (Ь == null || Ш.Count == 0) return; Vector3 Ś = new Vector3(); int Ř = 0; for (int A = 0; A < Ш.
        Count; A++) { if (Ш[A].WorldMatrix.Forward != М.WorldMatrix.Forward) continue; Ř++; Ś += Ш[A].GetPosition(); }
            Ś = Ś / Ř; Vector3 ŗ = Ȓ(Ь, Ś); Ь.
        Enabled = true; Ь.ShowOnHUD = ŝ; Ь.LeftExtend = (Ŝ ? 1 : ʃ) / 2f * Й - ŗ.X; Ь.RightExtend = (Ŝ ? 1 : ʃ) / 2f * Й + ŗ.X; ; Ь.TopExtend = (Ŝ ? 1 : ʂ) / 2f * Я + ŗ.Y; ; Ь.
        BottomExtend = (Ŝ ? 1 : ʂ) / 2f * Я - ŗ.Y; ; Ь.FrontExtend = (ŝ ? ʁ : ś) - ŗ.Z; Ь.BackExtend = ŝ ? 0 : Š + Ю * 0.75f + ŗ.Z; Ь.DetectFloatingObjects = true; Ь.
        DetectAsteroids = false; Ь.DetectLargeShips = true; Ь.DetectSmallShips = true; Ь.DetectStations = true; Ь.DetectOwner = true; Ь.DetectSubgrids = false; Ь
        .DetectPlayers = false; Ь.DetectEnemy = true; Ь.DetectFriendly = true; Ь.DetectNeutral = true;
        }
        void Ŗ<ŕ>(List<ŕ> Ă, bool e)
        {
            for (int A =
        0; A < Ă.Count; A++) Ŗ((IMyTerminalBlock)Ă[A], e);
        }
        void Ŕ(bool ř) { for (int A = 0; A < Ф.Count; A++) { Ф[A].Stockpile = ř; } }
        void Ţ<ŕ>(List<
        ŕ> Ă, bool Ů, float ŭ, bool Ŭ)
        {
            for (int A = 0; A < Ă.Count; A++)
            {
                IMyTextSurface O = null; if (Ă[A] is IMyTextSurface) O = (IMyTextSurface)Ă[
        A]; if (O != null) { O.ContentType = ContentType.TEXT_AND_IMAGE; if (!Ů) continue; O.Font = "Debug"; if (Ŭ) continue; O.FontSize = ŭ; }
            }
        }
        void
        Ŗ(IMyTerminalBlock q, bool e)
        {
            if (q == null) return; String ū = e ? "OnOff_On" : "OnOff_Off"; var Ū = q.GetActionWithName(ū); Ū.Apply(q);
        }
        bool ũ = true; void Ũ(bool e) { ũ = e; if (!ɽ) return; Ŗ(Х, e); }
        void ŧ(ChargeMode Ŧ) { for (int A = 0; A < Р.Count; A++) Р[A].ChargeMode = Ŧ; }
        void ť(List<IMyLandingGear> q, bool Ť) { for (int A = 0; A < q.Count; A++) { if (Ť) q[A].Lock(); if (!Ť) q[A].Unlock(); } }
        bool ţ = false; void ű(
        bool e)
        {
            if (ţ == e) return; List<IMyShipController> Ă = new List<IMyShipController>(); χ.GetBlocksOfType(Ă, Л); if (Ă.Count == 0) return;
            for (int A = 0; A < Ă.Count; A++) Ă[A].DampenersOverride = e; ţ = e;
        }
        IMyShipConnector Ò(MyShipConnectorStatus Ñ)
        {
            for (int A = 0; A < Ъ.Count; A
            ++) { if (!Ƃ(Ъ[A], true)) continue; if (Ъ[A].Status == Ñ) return Ъ[A]; }
            return null;
        }
        float Ð(Vector3 Ï, Vector3 Î, Vector3 Í, ͷ B)
        {
            if (Í.
            Length() == 0f) return 0; Vector3 Ó = ȕ(Ï, Î, Vector3.Normalize(Í)); float Ì = Â(-Ó, B); return Ì / Í.Length();
        }
        int Ê = 0; ͷ É = null; void È(bool
            Ç)
        {
            float Æ = 0; float Å = 0.9f; if (Ç)
            {
                Ƈ = -1; Ê = 0; É = null; if (Ӄ != ӌ.Ӌ && Ǟ.Í.Length() != 0)
                {
                    Æ = Å * Ð(Ǟ.Ï, Ǟ.ç * -1, Ǟ.Í, null); if (Æ < Ƈ || Ƈ == -1) Ƈ = Æ;
                }
                if (Β.Ͷ && Β.Í.Length() != 0) { Æ = Å * Ð(Β.Ï, Β.ç * -1, Β.Í, null); if (Æ < Ƈ || Ƈ == -1) Ƈ = Æ; }
                return;
            }
            if (Ê == -1) return; if (Ê >= 0)
            {
                int Ä = 0; while (Ê <
                ˋ.Count)
                {
                    if (Ä > 100) return; Ä++; ͷ B = ˋ[Ê]; if (B.Í.Length() != 0f)
                    {
                        Æ = Å * Math.Min(Ð(B.Ï, B.ç * -1, B.Í, B), Ð(B.Ï * -1, B.ç * -1, B.Í, B)); if (Æ <
                Ƈ || Ƈ == -1) Ƈ = Æ;
                    }
                    else É = B; Ê++;
                }
                Ê = -1;
            }
            bool Ã = true; float Ë = 0; if (ˋ.Count == 0 && Ƈ == -1) Ã = false; if (É != null)
            {
                for (int K = 0; K < ä.Count; K
                ++)
                {
                    String U = ä.Keys.ElementAt(K); float[,] I = ä.Values.ElementAt(K); float F = 0; if (!H(É, U, out F)) { Ã = false; break; }
                    for (int A = 0; A <
                I.GetLength(0); A++)
                    {
                        for (int ã = 0; ã < I.GetLength(1); ã++)
                        {
                            float â = Math.Abs(I[A, ã] * F); if (â == 0) continue; Ã = true; if (Ë == 0 || â < Ë) Ë = â
                ;
                        }
                    }
                }
            }
            if (!Ã)
            {
                for (int A = 0; A < Õ.GetLength(0); A++)
                {
                    for (int ã = 0; ã < Õ.GetLength(1); ã++)
                    {
                        float â = Math.Abs(Õ[A, ã]); if (â == 0) continue
                ; if (Ë == 0 || â < Ë) Ë = â;
                    }
                }
            }
            if (Ë > 0)
            {
                Æ = µ(Ë, Me.CubeGrid.GridSizeEnum == MyCubeSize.Small ? minAccelerationSmall : minAccelerationLarge);
                if (Æ > 0) if (Æ < Ƈ || Ƈ == -1) Ƈ = Æ;
            }
        }
        void á(bool à, float ß, float Þ, float Ý, float Ü)
        {
            for (int A = 0; A < Щ.Count; A++)
            {
                IMyGyro Û = Щ[A]; Û.GyroOverride = à; if (!à) Û.GyroPower = 100; else Û.GyroPower = ß; if (!à) continue; Vector3 Ï = Э.WorldMatrix.Forward; Vector3 Ú = Э.WorldMatrix.Right
                ; Vector3 Î = Э.WorldMatrix.Up; Vector3 Ù = Û.WorldMatrix.Forward; Vector3 Ø = Û.WorldMatrix.Up; Vector3 Ö = Û.WorldMatrix.Left * -1; 
                if
                (Ù == Ï) Û.SetValueFloat("Roll", Ü);
                else if (Ù == (Ï * -1)) Û.SetValueFloat("Roll", Ü * -1);
                else if (Ø == (Ï * -1)) Û.SetValueFloat("Yaw", Ü)
                ;
                else if (Ø == Ï) Û.SetValueFloat("Yaw", Ü * -1);
                else if (Ö == Ï) Û.SetValueFloat("Pitch", Ü);
                else if (Ö == (Ï * -1)) Û.SetValueFloat(
                "Pitch", Ü * -1); if (Ö == (Ú * -1)) Û.SetValueFloat("Pitch", Þ);
                else if (Ö == Ú) Û.SetValueFloat("Pitch", Þ * -1);
                else if (Ø == Ú) Û.SetValueFloat(
                "Yaw", Þ);
                else if (Ø == (Ú * -1)) Û.SetValueFloat("Yaw", Þ * -1);
                else if (Ù == (Ú * -1)) Û.SetValueFloat("Roll", Þ);
                else if (Ù == Ú) Û.
                SetValueFloat("Roll", Þ * -1); if (Ø == (Î * -1)) Û.SetValueFloat("Yaw", Ý);
                else if (Ø == Î) Û.SetValueFloat("Yaw", Ý * -1);
                else if (Ö == Î) Û.
                SetValueFloat("Pitch", Ý);
                else if (Ö == (Î * -1)) Û.SetValueFloat("Pitch", Ý * -1);
                else if (Ù == Î) Û.SetValueFloat("Roll", Ý);
                else if (Ù == (Î * -1)) Û.
                SetValueFloat("Roll", Ý * -1);
            }
        }
        float[,] Õ = new float[3, 2]; Dictionary<String, float[,]> ä = new Dictionary<string, float[,]>(); void D(
                IMyTerminalBlock q)
        {
            if (q == null) return; Õ = new float[3, 2]; ä = new Dictionary<string, float[,]>(); for (int A = 0; A < Δ.Count; A++)
            {
                IMyThrust K = Δ[A];
                if (!K.IsFunctional) continue; Vector3 Q = Ȑ(q, K.WorldMatrix.Backward); float P = K.MaxEffectiveThrust; if (Math.Round(Q.X, 2) != 0.0)
                    if (Q.X >= 0) Õ[0, 0] += P; else Õ[0, 1] -= P; if (Math.Round(Q.Y, 2) != 0.0) if (Q.Y >= 0) Õ[1, 0] += P; else Õ[1, 1] -= P; if (Math.Round(Q.Z, 2) != 0.0)
                    if (Q.Z >= 0) Õ[2, 0] += P; else Õ[2, 1] -= P; String O = L(K); float[,] N = null; if (ä.ContainsKey(O)) N = ä[O];
                else
                {
                    N = new float[3, 2]; ä.Add(O, N
                    );
                }
                float M = K.MaxThrust; if (Math.Round(Q.X, 2) != 0.0) if (Q.X >= 0) N[0, 0] += M; else N[0, 1] -= M; if (Math.Round(Q.Y, 2) != 0.0) if (Q.Y >= 0) N
                    [1, 0] += M;
                    else N[1, 1] -= M; if (Math.Round(Q.Z, 2) != 0.0) if (Q.Z >= 0) N[2, 0] += M; else N[2, 1] -= M;
            }
        }
        static String L(IMyThrust K)
        {
            return K.BlockDefinition.SubtypeId;
        }
        Vector3 S(Vector3 C, float[,] I)
        {
            return new Vector3(C.X >= 0 ? I[0, 0] : I[0, 1], C.Y >= 0 ? I[1, 0] : I[1, 1
            ], C.Z >= 0 ? I[2, 0] : I[2, 1]);
        }
        bool H(ͷ B, String G, out float F)
        {
            F = 0; int E = ˌ.IndexOf(G); if (E == -1 || B.Θ == null || E >= B.Θ.Length)
                return false; F = B.Θ[E]; if (F == -1) return false; return true;
        }
        Vector3 D(Vector3 C, ͷ B)
        {
            if (B != null)
            {
                Vector3 J = new Vector3(); for (int
                A = 0; A < ä.Keys.Count; A++)
                {
                    String U = ä.Keys.ElementAt(A); float F = 0; if (!H(B, U, out F)) { return S(C, Õ); }
                    J += S(C, ä.Values.ElementAt
                (A)) * F;
                }
                return J;
            }
            return S(C, Õ);
        }
        float Â(Vector3 C, ͷ B) { return Â(C, new Vector3(), B); }
        float Â(Vector3 C, Vector3 Á, ͷ B)
        {
            Vector3 Z = D(C, B); Vector3 À = Z + Á * ϋ; float º = (À / C).AbsMin(); return (float)(C * º).Length();
        }
        static float µ(float ª, float q)
        {
            if (q == 0)
                return 0; return ª / q;
        }
        void k(Vector3 h, bool e)
        {
            if (!e) { for (int A = 0; A < Δ.Count; A++) Δ[A].SetValueFloat("Override", 0.0f); return; }
            Vector3 Z = D(h, null); float Y = Math.Min(1, Math.Abs(µ(h.X, Z.X))); float X = Math.Min(1, Math.Abs(µ(h.Y, Z.Y))); float V = Math.Min(1, Math.
            Abs(µ(h.Z, Z.Z))); for (int A = 0; A < Δ.Count; A++)
            {
                IMyThrust K = Δ[A]; Vector3 Q = ȴ(Ȑ(Э, K.WorldMatrix.Backward), 1); if (Q.X != 0 && Math.
            Sign(Q.X) == Math.Sign(h.X)) K.SetValueFloat("Override", K.MaxThrust * Y);
                else if (Q.Y != 0 && Math.Sign(Q.Y) == Math.Sign(h.Y)) K.
            SetValueFloat("Override", K.MaxThrust * X);
                else if (Q.Z != 0 && Math.Sign(Q.Z) == Math.Sign(h.Z)) K.SetValueFloat("Override", K.MaxThrust * V);
                else
                    K.SetValueFloat("Override", 0.0f);
            }
        }
        float ē(Vector3 đ, Vector3 Đ, ͷ B)
        {
            if (đ.Length() == 0) return 0; float Å = 1; if (Đ.Length() > 0) Å
                    = Math.Min(1, Ȱ(-Đ, đ) / 90) * 0.4f + 0.6f; float ď = Â(đ, Đ, B); if (ď == 0) return 0.1f; float ª = µ(ď, ϋ); float K = (float)Math.Sqrt(µ(đ.Length
                    (), ª * 0.5f)); return ª * K * Å * ɵ;
        }
        bool Ď = false; bool č = false; bool Č = false; bool ċ = false; float Ċ = 0; float û = 0; Vector3 ü = new Vector3
                    (); Vector3 ĉ = new Vector3(); Vector3 Ĉ = new Vector3(1, 1, 1); float ć = 1; Vector3 Ć = new Vector3(); void ą()
        {
            Vector3 Ą = ĉ - ɉ; if (Ą.
                    Length() == 0) Ą = new Vector3(0, 0, -1); Vector3 ă = Ȑ(Э, Ą); Vector3 Ē = Vector3.Normalize(ă); Vector3 Á = Ȑ(Э, Э.GetNaturalGravity()); float ģ
                    = û > 0 ? Math.Max(0, 1 - Ȱ(Ą, ü) / 5) : 0; float Ĥ = (float)Math.Min((Ċ > 0 ? Ċ : 1000f), Math.Max(ē(-ă, Á, null), û * ģ)); if (!Ď) Ĥ = 0; if (č) Ĥ = Math.Max
                    (0, 1 - ė / Ė) * Ĥ; if (generalSpeedLimit > 0) Ĥ = Math.Min(generalSpeedLimit, Ĥ); if (ċ) Ĥ *= (float)Math.Min(1, µ(Ą.Length(), wpReachedDist) /
                    2); Vector3 Ģ = Ȑ(Э, Э.GetShipVelocities().LinearVelocity); float ġ = (float)(Math.Max(0, 15 - Ȱ(-Ē, -Ģ)) / 15) * 0.85f + 0.15f; ć += Math.
                    Sign(ġ - ć) / 10f; Vector3 Ġ = Ē * Ĥ * ć - (Ģ); Vector3 ĝ = D(Ġ, null); if (Č && Ԍ > 0.1f)
            {
                Ġ.X *= ğ(Ġ.X, ref Ĉ.X, 1f, ĝ.X, 20); Ġ.Y *= ğ(Ġ.Y, ref Ĉ.Y, 1f, ĝ.Y,
                    20); Ġ.Z *= ğ(Ġ.Z, ref Ĉ.Z, 1f, ĝ.Z, 20);
            }
            else Ĉ = new Vector3(1, 1, 1); Ć = ϋ * Ġ - Á * ϋ; k(Ć, Č); Ԍ = Vector3.Distance(ɉ, ĉ);
        }
        float ğ(float ª, ref
                    float ğ, float Ğ, float ĝ, float Ĝ)
        {
            ª = Math.Sign(Math.Round(ª, 2)); if (ª == Math.Sign(ğ)) ğ += Math.Sign(ğ) * Ğ; else ğ = ª; if (ª == 0) ğ = 1; float
                    J = Math.Abs(ğ); if (J < Ĝ || ĝ == 0) return J; ğ = Math.Min(Ĝ, Math.Max(-Ĝ, ğ)); J = Math.Abs(ĝ); return J;
        }
        bool ě = false; bool Ě = false; bool ę
                    = false; bool Ę = false; float ė = 0; float Ė = 2; Vector3 ĕ; Vector3 Ï; Vector3 ç; void Ĕ()
        {
            float Þ = 90; float Ü = 90; float Ý = 90; float ā = (
                    float)(Me.CubeGrid.GridSizeEnum == MyCubeSize.Small ? gyroSpeedSmall : gyroSpeedLarge) / 100f; Vector3 ð; Vector3 ï; Vector3 î; if (ę)
            {
                ð =
                    Vector3.Normalize(ĉ - ɉ); ï = Ȑ(Э, ð); î = Ȑ(Э, ç); Þ = Ȱ(ï, new Vector3(0, -1, 0)) - 90; Ü = Ȭ(î, new Vector3(-1, 0, 0), î.Y); Ý = Ȭ(ï, new Vector3(-1, 0, 0)
                    , ï.Z);
            }
            else
            {
                ð = Ï; î = Ȑ(Э, ç); ï = Ȑ(Э, Ï); Vector3 í = Ȑ(Э, ĕ); Þ = Ȭ(î, new Vector3(0, 0, 1), î.Y); Ü = Ȭ(î, new Vector3(-1, 0, 0), î.Y); Ý = Ȭ(í, new
                    Vector3(0, 0, 1), í.X);
            }
            if (Ę && ù())
            {
                Vector3 Í = Э.GetNaturalGravity(); î = Ȑ(Э, Í); Þ = Ȭ(î, new Vector3(0, 0, 1), î.Y); Ü = Ȭ(î, new Vector3(-1, 0, 0
                    ), î.Y);
            }
            if (!Ȧ(-45, Ü, 45)) { Þ = 0; Ý = 0; }; if (!Ȧ(-45, Ý, 45)) Þ = 0; á(Ě, 1, (-Þ) * ā, (-Ý) * ā, (-Ü) * ā); ė = Math.Max(Math.Abs(Þ), Math.Max(Math.
                    Abs(Ü), Math.Abs(Ý))); ě = ė <= Ė;
        }
        void ì() { this.Ě = false; }
        void ë(Vector3 ç, Vector3 Ï, Vector3 ê, float æ, bool é)
        {
            è(ç, æ, é); Ė = æ; ę =
                    false; this.Ï = Ï; this.ĕ = ê;
        }
        void ë(Vector3 ç, Vector3 Ï, Vector3 ê, bool é) { ë(ç, Ï, ê, 2f, é); }
        void è(Vector3 ç, float æ, bool é)
        {
            Ė = æ;
            this.Ě = true; this.Ę = é; ę = true; ě = false; this.ç = ç;
        }
        void å() { ñ(false, false, false, ĉ, 0); Č = false; }
        void ñ(Vector3 ý, float ú)
        {
            ñ(true,
            false, false, ý, ú);
        }
        void ñ(bool Ā, bool ÿ, bool þ, Vector3 ý, float ú) { ñ(Ā, ÿ, þ, ý, ý - ɉ, 0.0f, ú); }
        void ñ(bool Ā, bool ÿ, bool þ, Vector3 ý
            , Vector3 ü, float û, float ú)
        { Č = true; this.Ď = Ā; ĉ = ý; this.Ċ = ú; this.û = û; this.ċ = ÿ; this.č = þ; this.ü = ü; Ԍ = Vector3.Distance(ý, ɉ); }
        bool ù() { Vector3D ø; return this.Э.TryGetPlanetPosition(out ø); }
        Dictionary<String, float[]> ö = new Dictionary<string, float[]>();
        float õ; void ô() { if (!Ν) return; try { õ = Runtime.CurrentInstructionCount; } catch { } }
        void ó(String Ô)
        {
            if (!Ν) return; if (õ == 0) return; try
            {
                float Ɩ = (Runtime.CurrentInstructionCount - õ) / Runtime.MaxInstructionCount * 100; if (!ö.ContainsKey(Ô)) ö.Add(Ô, new float[]{Ɩ,Ɩ
});
                else { ö[Ô][0] = Ɩ; ö[Ô][1] = Math.Max(Ɩ, ö[Ô][1]); }
            }
            catch { }
        }
        string ǅ(float â) { return Math.Round(â, 2) + " "; }
        string ǅ(Vector3 â)
        { return "X" + ǅ(â.X) + "Y" + ǅ(â.Y) + "Z" + ǅ(â.Z); }
        Exception ȥ = null; void Ȥ()
        {
            String O =
        "Error occurred! \nPlease copy this and paste it \nin the \"Bugs and issues\" discussion.\n" + "Version: " + VERSION + "\n"; Ţ(П, setLCDFontAndSize, 0.9f, false); Ţ(О, setLCDFontAndSize, 0.9f, true); for (int A = 0; A < П.Count; A++) П
        [A].WriteText(O + ȥ.ToString()); for (int A = 0; A < О.Count; A++) О[A].WriteText(O + ȥ.ToString());
        }
        const String ȣ = "INSTRUCTIONS";
        const String Ȣ = "DEBUG"; String Ƕ = "", ȡ = ""; String Ǹ = ""; void ȟ()
        {
            String Ȟ = ""; String ǒ = ""; Ǹ = ϣ(false); Ȟ += Ǹ; ǒ += Ǹ; ǒ += γ(); for (int A = 0;
        A < П.Count; A++) П[A].WriteText(Ȟ); for (int A = 0; A < О.Count; A++) О[A].WriteText(Ȟ); Echo(ǒ); for (int A = 0; A < Н.Count; A++)
            {
                IMyTextPanel ƨ = Н[A]; String Ǩ = ƨ.CustomData.ToUpper(); if (Ǩ == Ȣ) ƨ.WriteText(Ƕ + "\n" + ȡ); if (Ǩ == ȣ) ƨ.WriteText(Ƞ());
            }
        }
        string Ƞ()
        {
            String O = "";
            try
            {
                float Ʊ = Runtime.MaxInstructionCount; O += "Inst: " + Runtime.CurrentInstructionCount + " Time: " + Math.Round(Runtime.
            LastRunTimeMs, 3) + "\n"; O += "Inst. avg/max: " + (int)(Χ * Ʊ) + " / " + (int)(Ψ * Ʊ) + "\n"; O += "Inst. avg/max: " + Math.Round(Χ * 100f, 1) + "% / " + Math.
            Round(Ψ * 100f, 1) + "% \n"; O += "Time avg/max: " + Math.Round(Υ, 2) + "ms / " + Math.Round(Φ, 2) + "ms \n";
            }
            catch { }
            for (int A = 0; A < ö.Count; A++)
            {
                O += "" + ö.Keys.ElementAt(A) + " = " + Math.Round(ö.Values.ElementAt(A)[0], 2) + " / " + Math.Round(ö.Values.ElementAt(A)[1], 2) +
            "%\n";
            }
            return O;
        }
        Vector3 ȴ(Vector3 ø, int ȳ) { return new Vector3(Math.Round(ø.X, ȳ), Math.Round(ø.Y, ȳ), Math.Round(ø.Z, ȳ)); }
        Vector3 Ȳ(Vector3 ø, float ȱ)
        {
            Vector3 J = new Vector3(Math.Sign(ø.X), Math.Sign(ø.Y), Math.Sign(ø.Z)); J.X = J.X == 0.0 ? ȱ : J.X; J.Y = J.Y ==
        0.0 ? ȱ : J.Y; J.Z = J.Z == 0.0 ? ȱ : J.Z; return J;
        }
        float Ȱ(Vector3 ȯ, Vector3 Ȫ)
        {
            if (ȯ == Ȫ) return 0; float ğ = (ȯ * Ȫ).Sum; float Ȯ = ȯ.Length();
            float ȭ = Ȫ.Length(); if (Ȯ == 0 || ȭ == 0) return 0; float J = (float)((180.0f / Math.PI) * Math.Acos(ğ / (Ȯ * ȭ))); if (float.IsNaN(J)) return 0;
            return J;
        }
        float Ȭ(Vector3 ȫ, Vector3 Ȫ, float ȩ) { float J = Ȱ(ȫ, Ȫ); if (ȩ > 0f) J *= -1; if (J > -90f) return J - 90f; else return 180f - (-J - 90f); }
        double Ȩ(float ȧ) { return (Math.PI / 180) * ȧ; }
        bool Ȧ(double ȵ, double ª, double ĝ) { return (ª >= ȵ && ª <= ĝ); }
        Vector3 Ȕ(IMyTerminalBlock q,
        Vector3 ȓ)
        { return Vector3D.Transform(ȓ, q.WorldMatrix); }
        Vector3 Ȓ(IMyTerminalBlock q, Vector3 ȑ) { return Ȑ(q, ȑ - q.GetPosition()); }
        Vector3 Ȑ(IMyTerminalBlock q, Vector3 ȏ) { return Vector3D.TransformNormal(ȏ, MatrixD.Transpose(q.WorldMatrix)); }
        Vector3 ȕ(Vector3
        ȍ, Vector3 Ȍ, Vector3 ȏ)
        { MatrixD ȋ = MatrixD.CreateFromDir(ȍ, Ȍ); return Vector3D.TransformNormal(ȏ, MatrixD.Transpose(ȋ)); }
        Vector3 Ȏ(Vector3 ȍ, Vector3 Ȍ, Vector3 đ) { MatrixD ȋ = MatrixD.CreateFromDir(ȍ, Ȍ); return Vector3D.Transform(đ, ȋ); }
        String Ȋ(Vector3
        ø)
        { return "" + ø.X + "|" + ø.Y + "|" + ø.Z; }
        Vector3 ȉ(String O)
        {
            String[] ȝ = O.Split('|'); return new Vector3(float.Parse(Ȗ(ȝ, 0)), float.
        Parse(Ȗ(ȝ, 1)), float.Parse(Ȗ(ȝ, 2)));
        }
        String Ȝ(ͷ B)
        {
            String ț = ":"; String J = Ȋ(B.ɉ) + ț + Ȋ(B.Ï) + ț + Ȋ(B.ç) + ț + Ȋ(B.ĕ) + ț + Ȋ(B.Í); for (int A =
        0; A < B.Θ.Length; A++) { J += ț; J += Math.Round(B.Θ[A], 3); }
            return J;
        }
        ͷ Ț(String ș)
        {
            String[] O = ș.Split(':'); ͷ J = new ͷ(ȉ(Ȗ(O, 0)), ȉ(Ȗ(
        O, 1)), ȉ(Ȗ(O, 2)), ȉ(Ȗ(O, 3)), ȉ(Ȗ(O, 4))); int A = 5; List<float> Ī = new List<float>(); while (A < O.Length)
            {
                String Ș = Ȗ(O, A); float â = 0;
                if (!float.TryParse(Ș, out â)) break; Ī.Add(â); A++;
            }
            J.Θ = Ī.ToArray(); return J;
        }
        void ȗ<ŕ>(ŕ O, bool Ư)
        {
            if (Ư) Storage += "\n"; Storage
                += O;
        }
        void ȗ<ŕ>(ŕ O) { ȗ(O, true); }
        String Ȗ(String[] O, int A)
        {
            String Ɨ = O.ElementAtOrDefault(A); if (String.IsNullOrEmpty(Ɨ))
                return ""; return Ɨ;
        }
        bool ɜ = false; void Save()
        {
            if (ɜ || ƽ == В.А) { Storage = ""; return; }
            Storage = DATAREV + ";"; ȗ(Ȋ(φ), false); ȗ(Ȋ(Β.Ï)); ȗ(Ȋ(Β
                .ĕ)); ȗ(Ȋ(Β.ç)); ȗ(Ȋ(Β.Í)); ȗ(Ȋ(Β.ɉ)); ȗ(Ȋ(Β.Ή)); ȗ(Β.Ͷ); ȗ(Ȋ(Ǟ.ɉ)); ȗ(Ȋ(Ǟ.Í)); ȗ(Ȋ(Ǟ.Ï)); ȗ(Ȋ(Ǟ.ç)); ȗ(Ȋ(Ǟ.ĕ)); ȗ(Ȋ(Ǟ.Ή)); ȗ(Ǟ.Ͷ); ȗ(
                Ȋ(Ӆ)); ȗ(Ȋ(ӄ)); ȗ(";"); ȗ((int)ƽ, false); ȗ((int)Ӄ); ȗ((int)ҟ); ȗ(ɹ); ȗ(ɸ); ȗ(ɷ); ȗ(ɶ); ȗ(ɵ); ȗ(ɿ); ȗ(ɺ); if (ƽ == В.Ͼ)
            {
                ȗ((int)ʔ.Ų); ȗ(ʔ.Ƅ)
                ; ȗ(ʔ.ƅ); ȗ(ʔ.Ɣ); ȗ(ʔ.ƕ); ȗ((int)ʒ.Ų); ȗ(ʒ.Ƅ); ȗ(ʒ.ƅ); ȗ(ʒ.Ɣ); ȗ(ʒ.ƕ);
            }
            else
            {
                ȗ((int)ʄ); ȗ((int)ʆ); ȗ((int)ɻ); ȗ((int)ʀ); ȗ((int)ӂ); ȗ(ʅ
                ); ȗ(ɽ); ȗ(ɾ); ȗ(ʇ); ȗ(ʃ); ȗ(ʂ); ȗ(ʁ); ȗ(ɴ); ȗ(ɳ); ȗ(ɼ); ȗ(ɲ); ȗ(Ӂ); ȗ(Ӏ); ȗ(ԓ); ȗ(Ԓ); ȗ(Ԑ); ȗ(ԑ); ȗ(Ҍ); ȗ(Ԁ);
            }
            ȗ(";"); for (int A = 0; A < ˌ.Count
                ; A++) ȗ((A > 0 ? "|" : "") + ˌ[A], false); ȗ(";"); for (int A = 0; A < ˋ.Count; A++) ȗ(Ȝ(ˋ[A]), A > 0);
        }
        ӌ ɛ = ӌ.Ӌ; public enum ɚ { ə, ɘ, ɗ, Ə }
        ɚ ɖ()
        {
            if (
                Storage == "") return ɚ.ɗ; String[] ɕ = Storage.Split(';'); if (Ȗ(ɕ, 0) != DATAREV) { return ɚ.Ə; }
            int A = 0; try
            {
                String[] O = Ȗ(ɕ, 1).Split('\n'); φ =
                ȉ(Ȗ(O, A++)); Β.Ï = ȉ(Ȗ(O, A++)); Β.ĕ = ȉ(Ȗ(O, A++)); Β.ç = ȉ(Ȗ(O, A++)); Β.Í = ȉ(Ȗ(O, A++)); Β.ɉ = ȉ(Ȗ(O, A++)); Β.Ή = ȉ(Ȗ(O, A++)); Β.Ͷ = bool.
                Parse(Ȗ(O, A++)); Ǟ.ɉ = ȉ(Ȗ(O, A++)); Ǟ.Í = ȉ(Ȗ(O, A++)); Ǟ.Ï = ȉ(Ȗ(O, A++)); Ǟ.ç = ȉ(Ȗ(O, A++)); Ǟ.ĕ = ȉ(Ȗ(O, A++)); Ǟ.Ή = ȉ(Ȗ(O, A++)); Ǟ.Ͷ = bool.
                Parse(Ȗ(O, A++)); Ӆ = ȉ(Ȗ(O, A++)); ӄ = ȉ(Ȗ(O, A++)); O = Ȗ(ɕ, 2).Split('\n'); A = 0; ƽ = (В)int.Parse(Ȗ(O, A++)); Ӄ = (ӌ)int.Parse(Ȗ(O, A++)); ҟ = (ӌ)
                int.Parse(Ȗ(O, A++)); ɹ = int.Parse(Ȗ(O, A++)); ɸ = int.Parse(Ȗ(O, A++)); ɷ = int.Parse(Ȗ(O, A++)); ɶ = int.Parse(Ȗ(O, A++)); ɵ = float.Parse(Ȗ
                (O, A++)); ɿ = bool.Parse(Ȗ(O, A++)); ɺ = bool.Parse(Ȗ(O, A++)); if (ƽ == В.Ͼ)
                {
                    ʔ.Ų = (ſ)int.Parse(Ȗ(O, A++)); ʔ.Ƅ = float.Parse(Ȗ(O, A++)); ʔ.
                ƅ = float.Parse(Ȗ(O, A++)); ʔ.Ɣ = Ȗ(O, A++); ʔ.ƕ = Ȗ(O, A++); ʒ.Ų = (ſ)int.Parse(Ȗ(O, A++)); ʒ.Ƅ = float.Parse(Ȗ(O, A++)); ʒ.ƅ = float.Parse(Ȗ(
                O, A++)); ʒ.Ɣ = Ȗ(O, A++); ʒ.ƕ = Ȗ(O, A++);
                }
                else
                {
                    ʄ = (ʲ)int.Parse(Ȗ(O, A++)); ʆ = (ʯ)int.Parse(Ȗ(O, A++)); ɻ = (ʪ)int.Parse(Ȗ(O, A++)); ʀ = (ʶ)
                int.Parse(Ȗ(O, A++)); ӂ = (ʲ)int.Parse(Ȗ(O, A++)); ʅ = bool.Parse(Ȗ(O, A++)); ɽ = bool.Parse(Ȗ(O, A++)); ɾ = bool.Parse(Ȗ(O, A++)); ʇ = bool.
                Parse(Ȗ(O, A++)); ʃ = int.Parse(Ȗ(O, A++)); ʂ = int.Parse(Ȗ(O, A++)); ʁ = int.Parse(Ȗ(O, A++)); ɴ = float.Parse(Ȗ(O, A++)); ɳ = float.Parse(Ȗ(O, A
                ++)); ɼ = float.Parse(Ȗ(O, A++)); ɲ = float.Parse(Ȗ(O, A++)); Ӂ = int.Parse(Ȗ(O, A++)); Ӏ = int.Parse(Ȗ(O, A++)); ԓ = int.Parse(Ȗ(O, A++)); Ԓ =
                int.Parse(Ȗ(O, A++)); Ԑ = int.Parse(Ȗ(O, A++)); ԑ = int.Parse(Ȗ(O, A++)); Ҍ = int.Parse(Ȗ(O, A++)); Ԁ = float.Parse(Ȗ(O, A++));
                }
                O = Ȗ(ɕ, 3).
                Replace("\n", "").Split('|'); ˌ = O.ToList(); O = Ȗ(ɕ, 4).Split('\n'); ˋ.Clear(); if (O.Count() >= 1 && O[0] != "") for (int ã = 0; ã < O.Length; ã++) ˋ.
                Add(Ț(Ȗ(O, ã)));
            }
            catch { return ɚ.ɘ; }
            ɛ = ҟ; Ҕ(); return ɚ.ə;
        }
        String ɔ(String Ǡ)
        {
            int A = Ǡ.IndexOf("//"); if (A != -1) Ǡ = Ǡ.Substring(0, A);
            String[] O = Ǡ.Split('='); if (O.Length <= 1) return ""; return O[1].Trim();
        }
        String ɓ(String[] O, String ǩ, ref bool Ã)
        {
            foreach (String ū in
            O) if (ū.StartsWith(ǩ)) return ū; Ã = false; return "";
        }
        bool ɯ = false; bool ɰ = false; String ɮ = ""; String ɭ = ""; IMyBroadcastListener ɬ =
            null; bool ɫ = true; void ɪ()
        {
            bool ɩ = true; if (ɫ)
            {
                ɫ = false; if (Me.CustomData.Contains("Antenna_Name"))
                {
                    ώ = "Update custom data"; Me.
            CustomData = "";
                }
            }
            String ɨ = (ƽ != В.А ? "[PAM-Ship]" : "[PAM-Controller]") + " Broadcast-settings"; try
            {
                if (Me.CustomData.Length == 0 || Me.
            CustomData.Split('\n')[0] != ɨ) ɣ(ɨ); String[] O = Me.CustomData.Split('\n'); ɯ = bool.Parse(ɔ(ɓ(O, "Enable_Broadcast", ref ɩ))); bool ɧ = false;
                bool ɦ = true; if (ɯ)
                {
                    if (ƽ != В.А) { ɮ = ɔ(ɓ(O, "Ship_Name", ref ɩ)).Replace(ɟ, ""); }
                    ɭ = ɔ(ɓ(O, "Broadcast_Channel", ref ɩ)).ToLower(); ɦ =
                false; if (ɬ == null) { ɬ = this.IGC.RegisterBroadcastListener(ɠ); ɬ.SetMessageCallback(""); }
                    List<IMyRadioAntenna> ɥ = new List<
                IMyRadioAntenna>(); χ.GetBlocksOfType(ɥ); bool ɤ = false; for (int A = 0; A < ɥ.Count; A++)
                    {
                        if (ɥ[A].EnableBroadcasting && ɥ[A].Enabled)
                        {
                            ɤ = true; break;
                        }
                    }
                    if (ɥ.Count == 0) ώ = "No Antenna found"; else if (!ɤ) ώ = "Antenna not ready"; ɧ = ɥ.Count == 0 || !ɤ; if (ɰ && !ɧ && ƽ != В.А) ώ = "Antenna ok";
                }
                else if (ƽ == В.А) ώ = "Offline - Enable in PB custom data"; ɰ = ɧ; if (ɦ) { if (ɬ != null) this.IGC.DisableBroadcastListener(ɬ); ɬ = null; }
            }
            catch { ɩ = false; }
            if (!ɩ) { ώ = "Reset custom data"; ɣ(ɨ); }
        }
        void ɣ(String ɢ)
        {
            String Ǩ = ɢ + "\n\n" + "Enable_Broadcast=" + (ƽ == В.А ? "true" :
            "false") + " \n"; Ǩ += ƽ != В.А ? "Ship_Name=Your_ship_name_here\n" : ""; Me.CustomData = Ǩ + "Broadcast_Channel=#default";
        }
        String ɡ()
        {
            if (ƽ != В.
            А) return "" + Me.GetId(); return ɟ;
        }
        const String ɠ = "[PAMCMD]"; const String ɟ = "#"; const Char ɞ = ';'; void ɝ(String ƿ, String ȷ)
        {
            ɀ
            (ƿ, ɟ, ȷ);
        }
        void ɀ(String ƿ, String ý, String ȷ)
        {
            try
            {
                if (!ɯ) return; ƿ = ɠ + ɞ + ɡ() + ɞ + ý + ɞ + ɭ + ɞ + ȷ + ɞ + ƿ; this.IGC.SendBroadcastMessage(ɠ, ƿ)
            ;
            }
            catch (Exception e) { ȥ = e; }
        }
        bool Ⱦ(String ƿ) { return ƿ.StartsWith(ɠ); }
        bool Ƚ(ref String ū, out string Ƹ, Char ȼ)
        {
            int A = ū.
            IndexOf(ȼ); Ƹ = ""; if (A < 0) return false; Ƹ = ū.Substring(0, A); ū = ū.Remove(0, A + 1); return true;
        }
        String ȿ = ""; bool Ȼ(ref String ƿ, out
            String ǋ, out String ȹ)
        {
            ǋ = ""; ȹ = ""; if (!ɯ) return false; String O = ""; if (!Ƚ(ref ƿ, out O, ɞ) || !Ⱦ(O)) return false; if (!Ƚ(ref ƿ, out ǋ, ɞ))
                return false; if (!Ƚ(ref ƿ, out O, ɞ) || (O != ɡ() && (O != "*" && ƽ != В.А))) return false; if (!Ƚ(ref ƿ, out O, ɞ) || (O != ɭ)) return false; if (!Ƚ(ref
                ƿ, out ȹ, ɞ)) return false; return true;
        }
        void ȸ(String ȷ)
        {
            if (!ɯ) return; String ȶ = "" + ɞ; String ƿ = ƿ = VERSION + ȶ; ƿ += ɮ + ȶ; ƿ += (int)ƽ + ȶ;
            ƿ += ǅ(ɉ.X) + "" + ȶ; ƿ += ǅ(ɉ.Y) + ȶ; ƿ += ǅ(ɉ.Z) + ȶ; if (ƽ != В.Ͼ) ƿ += Ϝ(ҟ) + (ҟ == ӌ.Ӊ ? " " + Math.Round(ӎ, 1) + "%" : "") + ȶ; else ƿ += Ϝ(ҟ) + ȶ; ƿ += ώ + ȶ; ƿ += Ѕ
            + "" + ȶ; ƿ += Ǹ + ȶ; ƿ += œ + ȶ; ƿ += ł + ȶ; for (int A = 0; A < ŀ.Count; A++) { if (ŀ[A].ĭ != ŉ.ň) continue; ƿ += ŀ[A].Ô + "/" + ŀ[A].ń + "/" + ŀ[A].Ń + ȶ; }
            ɝ(ƿ, ȷ);
        }
        public enum Ⱥ { ǻ, Ɂ, ɒ, ɑ }
        class ɐ
        {
            public DateTime ɏ; public DateTime Ɏ; public String ǃ = ""; public String Ô = ""; public String ɍ = "";
            public String Ɍ = ""; public String ɋ = ""; public String Ǹ = ""; public String Ɋ = ""; public Vector3 ɉ = new Vector3(); public List<Ņ> Ƿ = new
            List<Ņ>(); public В ƽ = В.ǻ; public Ⱥ Ɉ; public float ɇ; public float Ɇ; public int Ʋ; public int Ʌ = 0; public int Ʉ = 0; public bool Ƀ()
            { return (DateTime.Now - ɏ).TotalSeconds > 10; }
            public bool ɂ() { if (Ɉ != Ⱥ.Ɂ) return false; return (DateTime.Now - Ɏ).TotalSeconds >= 4; }
            public ɐ(String ǃ) { this.ǃ = ǃ; }
            public Ⱥ ǫ(String ǃ, bool ő, bool ǂ, bool ǁ)
            {
                if (ǃ == "" && !ǂ) return Ⱥ.ǻ; if (Ɉ == Ⱥ.Ɂ && ɂ()) Ɉ = Ⱥ.ɑ; if (ő)
                {
                    Ɋ = ǃ;
                    Ɉ = Ⱥ.Ɂ; Ɏ = DateTime.Now; return Ɉ;
                }
                else if (ǂ) { Ɋ = ""; Ɉ = Ⱥ.ǻ; } else if (Ɋ == ǃ) { if (ǁ) Ɉ = Ⱥ.ɒ; return Ɉ; }
                return Ⱥ.ǻ;
            }
        }
        void ǀ(ɐ Ʀ, String ƿ
                    )
        {
            Ʀ.ɏ = DateTime.Now; String Ä = ""; String ƾ = ""; String Ǆ = ""; String ƽ = ""; String[] ø = new string[3]; Ƚ(ref ƿ, out Ʀ.ɍ, ɞ); Ƚ(ref ƿ, out
                    Ʀ.Ô, ɞ); if (Ʀ.ɍ != VERSION) return; Ƚ(ref ƿ, out ƽ, ɞ); Ƚ(ref ƿ, out ø[0], ɞ); Ƚ(ref ƿ, out ø[1], ɞ); Ƚ(ref ƿ, out ø[2], ɞ); Ƚ(ref ƿ, out Ʀ.
                    ɋ, ɞ); Ƚ(ref ƿ, out Ʀ.Ɍ, ɞ); Ƚ(ref ƿ, out Ä, ɞ); Ƚ(ref ƿ, out Ʀ.Ǹ, ɞ); Ƚ(ref ƿ, out ƾ, ɞ); Ƚ(ref ƿ, out Ǆ, ɞ); Ʀ.Ƿ.Clear(); while (true)
            {
                String A; if (!Ƚ(ref ƿ, out A, ɞ)) break; String[] O = A.Split('/'); if (O.Count() < 3) continue; int ƻ = 0; if (!int.TryParse(O[2], out ƻ))
                    continue; Ʀ.Ƿ.Add(new Ņ(O[0], O[1], ƻ, ŉ.ň));
            }
            int ƺ = 0; if (!int.TryParse(Ä, out Ʀ.Ʋ)) Ʀ.Ʋ = 0; if (!int.TryParse(ƽ, out ƺ)) Ʀ.ƽ = В.ǻ; if (!float.
                    TryParse(ø[0], out Ʀ.ɉ.X)) Ʀ.ɉ.X = 0; if (!float.TryParse(ø[1], out Ʀ.ɉ.Y)) Ʀ.ɉ.Y = 0; if (!float.TryParse(ø[2], out Ʀ.ɉ.Z)) Ʀ.ɉ.Z = 0; if (!float
                    .TryParse(ƾ, out Ʀ.Ɇ)) Ʀ.Ɇ = 0; if (!float.TryParse(Ǆ, out Ʀ.ɇ)) Ʀ.ɇ = 0; Ʀ.ƽ = (В)ƺ; Ʀ.Ʌ = (int)Math.Round(Vector3.Distance(Me.
                    GetPosition(), Ʀ.ɉ)); Ʀ.Ʉ = 0; for (int ã = 0; ã < Ʀ.Ƿ.Count(); ã++) Ʀ.Ʉ += Ʀ.Ƿ[ã].Ń;
        }
        void ƹ(string Ƹ)
        {
            if (Ƹ == "") return; var ª = Ƹ.ToUpper().Split(' '
                    ); ª.DefaultIfEmpty(""); var Ƽ = ª.ElementAtOrDefault(0); var Ʒ = ª.ElementAtOrDefault(1); String ǆ = "Invalid argument: " + Ƹ; if (Є ==
                    ϸ.Ϭ)
            {
                if (ƞ != null)
                {
                    switch (Ƽ)
                    {
                        case "UP": { if (Ѕ < 2) break; if (ƞ.Ʋ == 0) { Ѕ = 1; return; } Ƨ(ƞ, "UP"); return; }
                        case "DOWN":
                            {
                                if (Ѕ < 1) break; if (Ѕ
                    == 1) { Ƨ(ƞ, "MRES"); break; }
                                Ƨ(ƞ, "DOWN"); break;
                            }
                        case "APPLY": { if (Ѕ < 2) break; Ƨ(ƞ, "APPLY"); return; }
                    }
                }
            }
            switch (Ƽ)
            {
                case "UP":
                    this.Ј(
                    false); return;
                case "DOWN": { this.Ї(false); return; }
                case "APPLY": this.Ɯ(true); return;
            }
            switch (Ƽ)
            {
                case "CLEAR": Ơ(); return;
                case
                    "SENDALL":
                    Ƨ(null, Ʒ); return;
                case "SEND": ǐ(Ƹ.Remove(0, "SEND".Length + 1)); return;
            }
            ώ = ǆ;
        }
        void ǐ(String Ƹ)
        {
            if (Ƹ == "") return; var ª = Ƹ.Split(
                    ':'); if (ª.Length != 2) { ώ = "Missing separator \":\""; return; }; ª.DefaultIfEmpty(""); String Ǐ = ª.ElementAtOrDefault(0).Trim(); ɐ Ʀ =
                    ǎ("", Ǐ); if (Ʀ != null) Ƨ(Ʀ, ª.ElementAtOrDefault(1).Trim()); else ώ = "Unknown ship: " + Ǐ;
        }
        ɐ ǎ(String ǃ, String Ô)
        {
            ǃ = ǃ.ToUpper(); Ô =
                    Ô.ToUpper(); for (int A = 0; A < Ǎ.Count; A++)
            {
                if (ǃ != "" && Ǎ[A].ǃ.ToUpper() == ǃ) return Ǎ[A]; if (Ô != "" && Ǎ[A].Ô.ToUpper() == Ô) return Ǎ[A
                    ];
            }
            return null;
        }
        List<ɐ> Ǎ = null; void ǌ(string Ƹ)
        {
            if (ρ) { Ǎ = new List<ɐ>(); }
            String ǋ = ""; String Ǌ = ""; if (!Ⱦ(Ƹ)) ƹ(Ƹ); if (ɬ != null && ɬ
                    .HasPendingMessage)
            {
                MyIGCMessage ǉ = ɬ.AcceptMessage(); String ž = (string)ǉ.Data; if (Ȼ(ref ž, out ǋ, out Ǌ) && ǋ != "" && ǋ != ɟ)
                {
                    ɐ Ʀ = ǎ(
                    ǋ, ""); if (Ʀ == null) { Ʀ = new ɐ(ǋ); Ǎ.Add(Ʀ); }
                    Ʀ.ǫ(Ǌ, false, false, true); ǀ(Ʀ, ž); Ǎ.Sort(delegate (ɐ Y, ɐ X) {
                        if (Y.Ô == null && X.Ô == null)
                            return 0;
                        else if (Y.Ô == null) return -1; else if (X.Ô == null) return 1; else return Y.Ô.CompareTo(X.Ô);
                    });
                }
            }
            if (ο || ρ)
            {
                if (Ϊ <= 0 || ρ)
                {
                    ώ = ""; ρ
                            = false; Ƥ(); ɪ(); ς = DateTime.Now;
                }
            }
            Ǘ();
        }
        Ⱥ ǈ(ɐ Ʀ, bool Ǉ, string ǃ)
        {
            Ⱥ J = Ⱥ.ǻ; if (Ʀ == null)
            {
                for (int A = 0; A < Ǎ.Count; A++)
                {
                    Ʀ = Ǎ[A]; Ⱥ Ƶ = Ʀ
                            .ǫ(ǃ, false, Ǉ, false); if (Ƶ == Ⱥ.ɒ) J = Ƶ; if (Ƶ == Ⱥ.ɑ) return Ƶ; if (Ƶ == Ⱥ.Ɂ) return Ƶ;
                }
                return J;
            }
            else return Ʀ.ǫ(ǃ, false, Ǉ, false);
        }
        void
                            Ƨ(ɐ Ʀ, String ƥ)
        {
            if (Ʀ == null) { for (int A = 0; A < Ǎ.Count; A++) Ǎ[A].ǫ(ƥ, true, false, false); ɀ(ƥ, "*", ƥ); }
            else
            {
                Ʀ.ǫ(ƥ, true, false, false)
                            ; ɀ(ƥ, Ʀ.ǃ, ƥ);
            }
        }
        void Ƥ()
        {
            List<IMyTerminalBlock> ƣ = new List<IMyTerminalBlock>(); χ.GetBlocksOfType(П, Л); χ.SearchBlocksOfName(
                            pamTag.Substring(0, pamTag.Length - 1) + ":", ƣ, q => q.CubeGrid == Me.CubeGrid && q is IMyTextSurfaceProvider); П = Ѧ(П, pamTag, true); ѵ(ƣ); э()
                            ; Ţ(П, setLCDFontAndSize, 1.15f, false); Ţ(О, setLCDFontAndSize, 1.15f, true); String Ƣ = ǳ(false); String ơ =
                            "//Custom Data is obsolete, please delete it.\n\n"; foreach (IMyTerminalBlock ƨ in П)
            {
                if (ƨ.CustomData == "") { ƨ.CustomData = ǳ(true); continue; }
                if (!ƨ.CustomData.Contains(Ƣ))
                {
                    if (!
                            ƨ.CustomData.Contains(ơ)) ƨ.CustomData = ơ + ƨ.CustomData;
                }
            }
        }
        void Ơ() { Ǎ.Clear(); ƞ = null; Љ(ϸ.Ϸ); Ѕ = 0; }
        ɐ ƞ = null; String Ɯ(bool ƛ)
        {
            int Ɲ = 0; return Ɯ(ƛ, 0, ref Ɲ, false, 1);
        }
        String Ɯ(bool ƛ, int ƚ, ref int ƙ, bool Ƙ, int Ɵ)
        {
            String Ɨ = ""; String Ʃ =
            "——————————————————\n"; String ƶ = "--------------------------------------------\n"; if (ƞ == null || Є == ϸ.Ϸ || Є == ϸ.ϻ) Ɨ += "[PAM]-Controller | " + Ǎ.Count +
            " Connected ships" + "\n";
            else Ɨ += Ʈ(ƞ) + "\n"; Ɨ += Ʃ; int A = 0; int ƴ = Ѕ; І = 0; if (ώ != "") { ύ(ref Ɨ, ƴ, A++, ƛ, ώ); }
            else if (Є == ϸ.Ϸ)
            {
                if (ƞ == null && Ǎ.Count >= 1) ƞ =
            Ǎ[0]; String Ƴ = ""; if (ύ(ref Ƴ, ƴ, A++, ƛ, " Send command to all")) Љ(ϸ.ϻ); int Ʋ = 0; for (int ã = 0; ã < Ǎ.Count; ã++)
                {
                    if (Ƙ) Ƴ += "\n"; ɐ Ʊ = Ǎ[
            ã]; if (ƴ == A || ƴ == A + 1) ƞ = Ʊ; if (A == ƴ) Ʋ = Ƴ.Split('\n').Length - 1; if (ύ(ref Ƴ, ƴ, A++, ƛ, " " + Ʈ(Ʊ)))
                    {
                        ƞ = Ʊ; Љ(ϸ.Ϭ); ƞ.ǫ("", false, true, false)
            ;
                    }
                    if (A == ƴ) Ʋ = Ƴ.Split('\n').Length - 1; if (ύ(ref Ƴ, ƴ, A++, ƛ, " " + (Ƙ ? "" : "  ") + ƭ(Ʊ))) { ƞ = Ʊ; Љ(ϸ.Ƿ); }
                }
                int ư = Ɵ - 2; ư += Math.Max(0, (ƚ - 1)) *
            Ɵ; Ɨ += ǧ(ư, Ƴ, Ʋ, ref ƙ);
            }
            else if (Є == ϸ.ž || Є == ϸ.ϻ)
            {
                ϸ J = Є == ϸ.ž ? ϸ.Ϭ : ϸ.Ϸ; ɐ Ʀ = null; if (Є == ϸ.ž) Ʀ = ƞ; if (ύ(ref Ɨ, ƴ, A++, ƛ, " Back"))
                {
                    Љ(J);
                }; if (ύ(ref Ɨ, ƴ, A++, ƛ, " [Stop] " + Ǔ(Ʀ, "STOP"))) { Ƨ(Ʀ, "STOP"); }; if (ύ(ref Ɨ, ƴ, A++, ƛ, " [Continue] " + Ǔ(Ʀ, "CONT"))) { Ƨ(Ʀ, "CONT"); }
            ; if (ύ(ref Ɨ, ƴ, A++, ƛ, " [Move home] " + Ǔ(Ʀ, "HOMEPOS"))) { Ƨ(Ʀ, "HOMEPOS"); }; if (ύ(ref Ɨ, ƴ, A++, ƛ, " [Move to job] " + Ǔ(Ʀ, "JOBPOS"))
            ) { Ƨ(Ʀ, "JOBPOS"); }; if (ύ(ref Ɨ, ƴ, A++, ƛ, " [Full simulation] " + Ǔ(Ʀ, "FULL"))) { Ƨ(Ʀ, "FULL"); }; if (Ʀ != null && Ʀ.ƽ == В.Ͼ) if (ύ(ref Ɨ, ƴ,
            A++, ƛ, " [Undock] " + Ǔ(Ʀ, "UNDOCK"))) { Ƨ(Ʀ, "UNDOCK"); };
            }
            else if (Є == ϸ.Ϭ)
            {
                String Ư = ""; if (ύ(ref Ư, ƴ, A++, ƛ, " Back")) { Љ(ϸ.Ϸ); }; Ɨ +=
            Ư.Substring(0, Ư.Length - 1).PadRight(36, ' '); if (ύ(ref Ɨ, ƴ, A++, ƛ, " Send cmd")) { Љ(ϸ.ž); }; if (!ƞ.Ƀ() && !ƞ.ɂ()) І++; if (ƞ.ɂ()) Ɨ +=
            "No answer received...";
                else Ɨ += ƪ(ƞ, ƞ.Ǹ);
            }
            else if (Є == ϸ.Ƿ) { if (ύ(ref Ɨ, ƴ, A++, ƛ, " Back")) { Љ(ϸ.Ϸ); }; Ɨ += ƶ; Ɨ += ƪ(ƞ, ǘ(ƞ)); }
            if (!Њ) Ѕ = Math.Min(І - 1, Ѕ);
            return Ɨ;
        }
        String Ʈ(ɐ Ʀ) { if (Ʀ.ɍ != VERSION) return Ʀ.Ô + ": Different version!"; return Ʀ.Ô + ": " + ƪ(Ʀ, Ʀ.ɋ) + " " + Ʀ.Ʌ + "m"; ; }
        String ƭ(ɐ Ʀ)
        {
            String O = ǿ("", µ(Ʀ.Ɇ, Ʀ.ɇ) * 100f, 100f, 8, 0, 0) + "% "; for (int A = 0; A < Ʀ.Ƿ.Count; A++)
            {
                if (A >= 5) break; Ņ Ĵ = Ʀ.Ƿ[A]; O += Ǜ(Ȁ(Ƭ(Ĵ)), 3) + " "
        + ǖ(Ĵ.Ń) + " ";
            }
            return O;
        }
        String Ƭ(Ņ Ĵ)
        {
            if (Ĵ.ń.ToUpper() == "ORE" || Ĵ.ń.ToUpper() == "INGOT")
            {
                String ƫ = GetElementCode(Ĵ.Ô.ToUpper
        ()); if (ƫ != "") return ƫ;
            }
            return Ĵ.Ô;
        }
        String ƪ(ɐ Ʀ, String O)
        {
            if (Ʀ.Ƀ()) return "No signal...(" + Ǖ((int)(DateTime.Now - Ʀ.ɏ).
        TotalSeconds) + ")"; return O;
        }
        public enum Ǒ { ǻ, ǹ, Ǹ, Ƿ, Ƕ, ǵ, Ǵ }
        String ǳ(bool ǲ)
        {
            String J = ""; if (ǲ) J += "mode=main:1\n\n"; J +=
        "//Available modes:\n" + "//main:<Page>\n" + "//mainX:<Page>  (no empty lines)\n" + "//menu\n" + "//inventory\n" + "//menu:<shipname>\n" +
        "//inventory:<shipname>"; return J;
        }
        void Ǳ(IMyTerminalBlock ƨ, out Ǒ ǰ, out String ǩ)
        {
            bool Ɲ = true; String ƽ = ɔ(ɓ(ƨ.CustomData.Split('\n'), "mode", ref
        Ɲ)).ToUpper(); String[] ǯ = ƽ.Split(':'); String Ǯ = ǭ(ǯ, 0); ǩ = ǭ(ǯ, 1); ǰ = Ǒ.ǻ; if (Ǯ == "MAIN") ǰ = Ǒ.ǹ;
            else if (Ǯ == "MAINX") ǰ = Ǒ.Ǵ;
            else if (Ǯ
        == "MENU") ǰ = Ǒ.Ǹ;
            else if (Ǯ == "INVENTORY") ǰ = Ǒ.Ƿ; else if (Ǯ == "DEBUG") ǰ = Ǒ.Ƕ; if (ǰ == Ǒ.ǻ) ǩ = ƽ;
        }
        String ǭ(String[] ª, int E)
        {
            if (E < ª.
        Length) return ª[E].Trim(); return "";
        }
        int Ǭ = 0; int Ǻ = 0; int Ǽ = 0; int ȇ = 0; int Ȉ = 0; int Ȇ = 0; int ȅ = 0; String Ȅ(Ǒ ƽ, String ǩ, bool Ç)
        {
            int
        ȃ = 15; if (Ç) { Ǻ = Ǭ; Ǭ = 0; ȇ = Ǽ; Ǽ = 0; }
            String J = ""; if (ƽ == Ǒ.ǹ)
            {
                int E = 0; J = Ɯ(false, Ǻ, ref Ȉ, true, ȃ); if (!int.TryParse(ǩ, out E)) return J; Ǭ
        = Math.Max(E, Ǭ); E--; return Ǣ(E, ȃ, J);
            }
            else if (ƽ == Ǒ.Ǵ)
            {
                int E = 0; J = Ɯ(false, ȇ, ref Ȇ, false, ȃ); if (!int.TryParse(ǩ, out E)) return J
        ; Ǽ = Math.Max(E, Ǽ); E--; return Ǣ(E, ȃ, J);
            }
            if (ƽ == Ǒ.ǵ) { return Ɯ(false, 0, ref ȅ, true, ȃ); }
            else if (ƽ == Ǒ.Ƿ || ƽ == Ǒ.Ǹ || ƽ == Ǒ.Ƕ)
            {
                ɐ Ʀ = ƞ; if
        (ǩ != "") { Ʀ = ǎ("", ǩ); if (Ʀ == null) return "Unknown ship: " + ǩ; }
                else
                {
                    if (Ʀ == null) return "No ship on main screen selected."; J =
        "Selected: ";
                }
                String Ʃ = "—————————————————————\n"; String Ȃ = ""; String ȁ = ""; if (ƽ == Ǒ.Ǹ) { Ȃ = "Menu"; ȁ = ƪ(Ʀ, Ʀ.Ǹ); }
                else if (ƽ == Ǒ.Ƿ)
                {
                    Ȃ =
        "Inventory"; ȁ = ƪ(Ʀ, ǘ(Ʀ));
                }
                else if (ƽ == Ǒ.Ƕ) { return ƪ(Ʀ, Ǚ(Ʀ)); }
                J += Ʀ.Ô + " - " + Ʀ.Ʌ + "m | " + Ȁ("" + Ȃ) + "\n" + Ʃ; J += ȁ; return J;
            }
            return
        "Unknown command: " + ǩ; ;
        }
        String Ȁ(String O) { if (O == "") return O; return O.First().ToString().ToUpper() + O.Substring(1).ToLower(); }
        string ǿ(
        String Ô, float Ń, float ĝ, int Ǿ, int ǽ, int Ǫ)
        {
            float ǜ = µ(Ń, ĝ) * Ǿ; String O = "["; for (int A = 0; A < Ǿ; A++) { if (A <= ǜ) O += "|"; else O += "'"; }
            O +=
        "]"; return O + " " + Ǜ(Ȁ(Ô), ǽ).PadRight(ǽ) + "".PadRight(Ǫ) + ǖ(Ń);
        }
        String Ǜ(String O, int ǚ)
        {
            if (O == "") return O; if (O.Length > ǚ) O = O.
        Substring(0, ǚ - 1) + "."; return O;
        }
        string Ǚ(ɐ Ʀ)
        {
            String O = ""; O += Ʀ.ɍ + "\n"; O += Ʀ.Ô + "\n"; O += Ʀ.ɉ.X + "\n"; O += Ʀ.ɉ.Y + "\n"; O += Ʀ.ɉ.Z + "\n"; O += Ʀ.ɋ
        + "\n"; O += Ʀ.Ɍ + "\n"; O += Ʀ.Ʋ + "\n"; O += Ʀ.Ǹ.Length + "\n"; O += Ʀ.Ɇ + "\n"; O += Ʀ.ɇ + "\n"; O += Ʀ.Ƿ.Count() + "\n"; return O;
        }
        string ǘ(ɐ Ʀ)
        {
            String O = ""; O += ǿ("All", µ(Ʀ.Ɇ, Ʀ.ɇ) * 100f, 100, 30, 8, 12) + "%\n"; O += "\n"; for (int ã = 0; ã < Ʀ.Ƿ.Count(); ã++)
            {
                Ņ Ĵ = Ʀ.Ƿ[ã]; O += ǿ(Ĵ.Ô, Ĵ.Ń, Ʀ.Ʉ,
            30, 8, 10) + "\n";
            }
            return O;
        }
        String ǖ(float Ń)
        {
            if (Ń >= 1000000) return Math.Round(Ń / 1000000f, Ń / 1000000f < 100 ? 1 : 0) + "M"; if (Ń >= 1000)
                return Math.Round(Ń / 1000f, Ń / 1000f < 100 ? 1 : 0) + "K"; return "" + Math.Round(Ń);
        }
        String Ǖ(int ǔ)
        {
            if (ǔ >= 60 * 60) return Math.Round(ǔ / (60f *
                60f), 1) + " h"; if (ǔ >= 60) return Math.Round(ǔ / 60f, 1) + " min"; return "" + ǔ + " s";
        }
        String Ǔ(ɐ Ʀ, String ž)
        {
            Ⱥ Ƶ = ǈ(Ʀ, false, ž); if (Ƶ == Ⱥ.ɒ)
                return "received!"; if (Ƶ == Ⱥ.Ɂ) return "pending..."; if (Ƶ == Ⱥ.ɑ) return "no answer!"; return "";
        }
        void Ǘ()
        {
            String ǒ = "[PAM]-Controller\n\n"
                + "Run-arguments: (Type without:[ ])\n" + "———————————————\n" + "[UP] Menu navigation up\n" + "[DOWN] Menu navigation down\n" +
                "[APPLY] Apply menu point\n" + "[CLEAR] Clear miner list\n" + "[SEND ship:cmd] Send to a ship*\n" + "[SENDALL cmd] Send to all ships*\n" +
                "———————————————\n\n" + "*[SEND] = Cmd to one ship:\n" + " e.g.: \"SEND Miner 1:homepos\"\n\n" + "*[SENDALL] = Cmd to all ships:\n" +
                " e.g.: \"SENDALL homepos\"\n\n"; for (int A = 0; A < О.Count; A++) О[A].WriteText(Ȅ(Ǒ.ǵ, "0", false)); for (int A = 0; A < П.Count; A++)
            {
                Ǒ ƽ = Ǒ.ǻ; String ǩ = ""; Ǳ(П[A], out ƽ,
                out ǩ); П[A].WriteText(Ȅ(ƽ, ǩ, A == 0));
            }
            Echo(ǒ); for (int A = 0; A < Н.Count; A++)
            {
                IMyTextPanel ƨ = Н[A]; String Ǩ = ƨ.CustomData.ToUpper();
                if (Ǩ == Ȣ) ƨ.WriteText(Ƕ + "\n" + ȡ); if (Ǩ == ȣ) ƨ.WriteText(Ƞ());
            }
        }
        String ǧ(int Ǧ, String ū, int ǥ, ref int Ǥ)
        {
            String[] ǟ = ū.Split('\n');
            if (ǥ >= Ǥ + Ǧ - 1) Ǥ++; Ǥ = Math.Min(ǟ.Count() - 1 - Ǧ, Ǥ); if (ǥ < Ǥ + 1) Ǥ--; Ǥ = Math.Max(0, Ǥ); String J = ""; for (int A = 0; A < Ǧ; A++)
            {
                int ǣ = A + Ǥ; if (ǣ >=
            ǟ.Count()) break; J += ǟ[ǣ] + "\n";
            }
            return J;
        }
        String Ǣ(int E, int ǡ, String Ǡ)
        {
            String[] ǟ = Ǡ.Split('\n'); int Ǟ = E * ǡ; int ǝ = (E + 1) * (ǡ);
            String J = ""; for (int A = Ǟ; A < ǝ; A++) { if (A >= ǟ.Count()) break; J += ǟ[A] + "\n"; }
            return J;
        }


    }
}
