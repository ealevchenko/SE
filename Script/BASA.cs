using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace TEST1
{
    /// <summary>
    /// Скрипт управления шахтеромШАХТЕР2
    /// </summary>
    public sealed class Program : MyGridProgram
    {
        List<IMyFunctionalBlock> list_block = new List<IMyFunctionalBlock>();                  // Список всех блоков       
        IMyTextPanel krot1_lsd;
        IMyShipDrill krot1_drl1, krot1_drl2, krot1_drl3, krot1_drl4, krot1_drl5;
        IMyPistonBase krot1_pst_lapa1;
        IMyShipConnector krot1_con1;
        IMyBatteryBlock krot1_batt1;
        IMyMotorStator krot1_shar1;

        float min_krot1_shar1 = -30.0f;       // мин уставка
        float max_krot1_shar1 = 30.0f;        // мак уставка
        float speed_krot1_shar1 = 1.0f;       // один оборот в минуту
        float speed_krot1_pst_lapa1_extend = 0.05f;          // Скорость выдвигания бура
        float speed_krot1_pst_lapa1_retract = 1.0f;          // Скорость втягивания бура

        int pr = 0; // Номер программы
        int p = 0;  // Номер подпрограммы
        int pp = 0;  // Номер подпрограммы

        float pos_krot1_shar1 = 0f;  // позиция шарнира
        float pos_krot1_pst_lapa1 = 0f;  // позиция поршня
        int delay = 0;

        public Program()
        {

            Runtime.UpdateFrequency = UpdateFrequency.Update10;
            GridTerminalSystem.GetBlocksOfType<IMyFunctionalBlock>(list_block, r => r.CustomName.Contains("БАЗА1-"));
            foreach (IMyFunctionalBlock obj in list_block)
            {
                if (obj.CustomName.Contains("БАЗА1-Дисплей [КРОТ-1]")) krot1_lsd = (IMyTextPanel)obj;
                if (obj.CustomName.Contains("БАЗА1-Бур 1 [КРОТ-1]")) krot1_drl1 = (IMyShipDrill)obj;
                if (obj.CustomName.Contains("БАЗА1-Бур 2 [КРОТ-1]")) krot1_drl2 = (IMyShipDrill)obj;
                if (obj.CustomName.Contains("БАЗА1-Бур 3 [КРОТ-1]")) krot1_drl3 = (IMyShipDrill)obj;
                if (obj.CustomName.Contains("БАЗА1-Бур 4 [КРОТ-1]")) krot1_drl4 = (IMyShipDrill)obj;
                if (obj.CustomName.Contains("БАЗА1-Бур 5 [КРОТ-1]")) krot1_drl5 = (IMyShipDrill)obj;
                if (obj.CustomName.Contains("БАЗА1-Поршень-лапа [КРОТ-1]")) krot1_pst_lapa1 = (IMyPistonBase)obj;
                if (obj.CustomName.Contains("БАЗА1-Шарнир-лапа [КРОТ-1]")) krot1_shar1 = (IMyMotorStator)obj;
                if (obj.CustomName.Contains("БАЗА1-Коннектор [КРОТ-1]")) krot1_con1 = (IMyShipConnector)obj;
                if (obj.CustomName.Contains("БАЗА1-Батарея [КРОТ-1]")) krot1_batt1 = (IMyBatteryBlock)obj;
            }
            Echo("lsd_krot1: " + ((krot1_lsd != null) ? ("Ок") : ("not found")));
            Echo("krot1_drl1: " + ((krot1_drl1 != null) ? ("Ок") : ("not found")));
            Echo("krot1_drl2: " + ((krot1_drl2 != null) ? ("Ок") : ("not found")));
            Echo("krot1_drl3: " + ((krot1_drl3 != null) ? ("Ок") : ("not found")));
            Echo("krot1_drl4: " + ((krot1_drl4 != null) ? ("Ок") : ("not found")));
            Echo("krot1_drl5: " + ((krot1_drl5 != null) ? ("Ок") : ("not found")));
            Echo("krot1_pst_lapa1: " + ((krot1_pst_lapa1 != null) ? ("Ок") : ("not found")));
            Echo("krot1_shar1: " + ((krot1_shar1 != null) ? ("Ок") : ("not found")));
            Echo("krot1_con1: " + ((krot1_con1 != null) ? ("Ок") : ("not found")));
            Echo("krot1_batt1: " + ((krot1_batt1 != null) ? ("Ок") : ("not found")));


            Krot1_HandRetract();
            Krot1_SetSharnir(0);
            Krot1_Drill_Off();
            Krot1_SL_Off();

            ClearText();
            pr = 0; // Номер программы
            p = 0;  // Номер подпрограммы
            pp = 0; // Номер уровня подпрограммы

            pos_krot1_shar1 = 0f;  // позиция шарнира
            pos_krot1_pst_lapa1 = 0f;  // позиция поршня
            delay = 0;

        }
        public void Save()
        {

        }
        public void Main(string argument, UpdateType updateSource)
        {
            OutText(String.Format("Main arg={0} up={1} pr={2}, p={3}, pp={4}\n", argument, updateSource, pr, p, pp));
            if (updateSource == UpdateType.Update10)
            {
                if (pr == 1)
                {
                    Krot1_Bur();
                }
            }
            else
            {
                if (argument == "pr:1")
                {
                    OutText("Старт(Krot1_Bur):" + "\n");
                    pr = 1; // Номер программы
                    p = 0;  // Номер подпрограммы
                    pos_krot1_shar1 = 0f;  // позиция шарнира
                    pos_krot1_pst_lapa1 = 0f;  // позиция поршня
                    delay = 0;
                }
            }
            return;

        }
        //-----------------------------------------
        // Программы
        void Krot1_Bur()
        {
            switch (pp)
            {
                case 0: { Krot1_SL_On(); pp++; return; }
                case 1: { Bur_Pozition_Drill(0); return; }
                case 2: { Bur_Pozition_Drill(5); return; }
                case 3: { Bur_Pozition_Drill(-5); return; }
                case 4: { Bur_Pozition_Drill(10); return; }
                case 5: { Bur_Pozition_Drill(-10); return; }
                case 6: { Bur_Pozition_Drill(15); return; }
                case 7: { Bur_Pozition_Drill(-15); return; }
                case 8: { Bur_Pozition_Drill(20); return; }
                case 9: { Bur_Pozition_Drill(-20); return; }
                case 10: { Bur_Pozition_Drill(25); return; }
                case 11: { Bur_Pozition_Drill(-25); return; }
                case 12: { Bur_Pozition_Drill(30); return; }
                case 13: { Bur_Pozition_Drill(-30); return; }
                case 14: { p_set_pos_krot1_shar1(0); pp++; return; }
                case 15: { Krot1_Drill_Off(); pp++; return; }
                case 16: { Krot1_SL_Off(); pp++; return; }
            }
        }
        // Бурим с позиции
        void Bur_Pozition_Drill(float position)
        {
            switch (p)
            {
                case 0: { p_set_pos_krot1_shar1(position); return; }
                case 1: { Krot1_Drill_On(); p++; return; }
                case 2: { p_delay(30); return; }
                case 3: { Krot1_HandExtend(0.5f); p++; return; }
                case 4: { p_get_pos_krot1_pst_lapa1_max(); return; }
                case 5: { p_delay(40); return; }
                case 6: { Krot1_HandExtend(1.0f); p++; return; }
                case 7: { p_get_pos_krot1_pst_lapa1_max(); return; }
                case 8: { p_delay(40); return; }
                case 9: { Krot1_HandExtend(1.5f); p++; return; }
                case 10: { p_get_pos_krot1_pst_lapa1_max(); return; }
                case 11: { p_delay(40); return; }
                case 12: { Krot1_HandExtend(2.0f); p++; return; }
                case 13: { p_get_pos_krot1_pst_lapa1_max(); return; }
                case 14: { p_delay(40); return; }
                case 15: { Krot1_HandExtend(2.5f); p++; return; }
                case 16: { p_get_pos_krot1_pst_lapa1_max(); return; }
                case 17: { p_delay(40); return; }
                case 18: { Krot1_HandExtend(3.0f); p++; return; }
                case 19: { p_get_pos_krot1_pst_lapa1_max(); return; }
                case 20: { p_delay(40); return; }
                case 21: { Krot1_HandExtend(3.5f); p++; return; }
                case 22: { p_get_pos_krot1_pst_lapa1_max(); return; }
                case 23: { p_delay(40); return; }
                case 24: { Krot1_HandExtend(4.0f); p++; return; }
                case 25: { p_get_pos_krot1_pst_lapa1_max(); return; }
                case 26: { p_delay(40); return; }
                case 27: { Krot1_HandExtend(4.5f); p++; return; }
                case 28: { p_get_pos_krot1_pst_lapa1_max(); return; }
                case 29: { p_delay(40); return; }
                case 30: { Krot1_HandExtend(5.0f); p++; return; }
                case 31: { p_get_pos_krot1_pst_lapa1_max(); return; }
                case 32: { p_delay(40); return; }
                case 33: { Krot1_HandExtend(5.5f); p++; return; }
                case 34: { p_get_pos_krot1_pst_lapa1_max(); return; }
                case 35: { p_delay(40); return; }
                case 36: { Krot1_HandExtend(6.0f); p++; return; }
                case 37: { p_get_pos_krot1_pst_lapa1_max(); return; }
                case 38: { p_delay(40); return; }
                case 39: { Krot1_HandExtend(6.5f); p++; return; }
                case 40: { p_get_pos_krot1_pst_lapa1_max(); return; }
                case 41: { p_delay(40); return; }
                case 42: { Krot1_HandExtend(7.0f); p++; return; }
                case 43: { p_get_pos_krot1_pst_lapa1_max(); return; }
                case 44: { p_delay(40); return; }
                case 45: { Krot1_HandExtend(7.5f); p++; return; }
                case 46: { p_get_pos_krot1_pst_lapa1_max(); return; }
                case 47: { p_delay(40); return; }
                case 48: { Krot1_HandExtend(8.0f); p++; return; }
                case 49: { p_get_pos_krot1_pst_lapa1_max(); return; }
                case 50: { p_delay(40); return; }
                case 51: { Krot1_HandExtend(8.5f); p++; return; }
                case 52: { p_get_pos_krot1_pst_lapa1_max(); return; }
                case 53: { p_delay(40); return; }
                case 54: { Krot1_HandExtend(9.0f); p++; return; }
                case 55: { p_get_pos_krot1_pst_lapa1_max(); return; }
                case 56: { p_delay(40); return; }
                case 57: { Krot1_HandExtend(9.5f); p++; return; }
                case 58: { p_get_pos_krot1_pst_lapa1_max(); return; }
                case 59: { p_delay(40); return; }
                case 60: { Krot1_HandExtend(10.0f); p++; return; }
                case 61: { p_get_pos_krot1_pst_lapa1_max(); return; }
                case 62: { p_delay(40); return; }
                case 63: { Krot1_HandRetract(); p++; return; }
                case 64: { p_get_pos_krot1_pst_lapa1_min(); return; }
                case 65: { pp++; p = 0; return; }
            }
        }
        void p_set_pos_krot1_shar1(float position)
        {
            pos_krot1_shar1 = position;
            Krot1_SetSharnir(pos_krot1_shar1);
            p++;
            return;
        }
        void p_get_pos_krot1_pst_lapa1_max()
        {
            float piston_left1_position = krot1_pst_lapa1.CurrentPosition;
            if (piston_left1_position == pos_krot1_pst_lapa1)
            {
                p++;
            }
            return;
        }
        void p_get_pos_krot1_pst_lapa1_min()
        {
            float piston_left1_position = krot1_pst_lapa1.CurrentPosition;
            if (piston_left1_position == 0)
            {
                pos_krot1_pst_lapa1 = 0;
                p++;
            }
            return;
        }
        void p_delay(int max_delay)
        {
            delay++;
            if (delay >= max_delay)
            {
                delay = 0;
                p++;
            }
            return;
        }
        //-----------------------------------------
        public void OutText(string text)
        {
            if (krot1_lsd != null)
            {
                krot1_lsd.WriteText(text, true);
            }
        }
        public void ClearText()
        {
            if (krot1_lsd != null)
            {
                krot1_lsd.WriteText("", false);
            }
        }
        double RadToGradus(float rad)
        {
            return rad * 180 / Math.PI;
        }
        void SetRotate(float degrees, IMyMotorStator mortor, float min, float max, float speed)
        {
            mortor.TargetVelocityRPM = 0;
            // Текущее положение
            double motor_curennt_grad = RadToGradus(mortor.Angle);

            if (degrees < motor_curennt_grad)
            {
                // Движим влево
                // Если задали меньше чем уставка тогда движем
                if (degrees > min)
                {
                    mortor.LowerLimitDeg = degrees;
                    mortor.TargetVelocityRPM = speed * -1;
                }
            }
            else
            {
                // Движим вправо
                // Если задали меньше чем уставка тогда движем
                if (degrees < max)
                {
                    mortor.UpperLimitDeg = degrees;
                    mortor.TargetVelocityRPM = speed;
                }
            };


        }
        void Krot1_SetSharnir(float degrees)
        {
            if (krot1_shar1 != null)
                SetRotate(degrees, krot1_shar1, min_krot1_shar1, max_krot1_shar1, speed_krot1_shar1);
        }
        void Krot1_Drill_Off()
        {
            if (krot1_drl1 != null && krot1_drl2 != null && krot1_drl3 != null && krot1_drl4 != null && krot1_drl5 != null)
            {
                krot1_drl1.ApplyAction("OnOff_Off");
                krot1_drl2.ApplyAction("OnOff_Off");
                krot1_drl3.ApplyAction("OnOff_Off");
                krot1_drl4.ApplyAction("OnOff_Off");
                krot1_drl5.ApplyAction("OnOff_Off");
            }
        }
        void Krot1_Drill_On()
        {
            if (krot1_drl1 != null && krot1_drl2 != null && krot1_drl3 != null && krot1_drl4 != null && krot1_drl5 != null)
            {
                krot1_drl1.ApplyAction("OnOff_On");
                krot1_drl2.ApplyAction("OnOff_On");
                krot1_drl3.ApplyAction("OnOff_On");
                krot1_drl4.ApplyAction("OnOff_On");
                krot1_drl5.ApplyAction("OnOff_On");
            }
        }
        void Krot1_HandExtend()
        {
            if (krot1_pst_lapa1 != null)
            {
                krot1_pst_lapa1.Velocity = speed_krot1_pst_lapa1_extend;
                krot1_pst_lapa1.Extend();
            }
        }
        void Krot1_HandExtend(float position)
        {
            if (krot1_pst_lapa1 != null)
            {
                pos_krot1_pst_lapa1 = position;
                krot1_pst_lapa1.Velocity = speed_krot1_pst_lapa1_extend;
                krot1_pst_lapa1.MaxLimit = position;
                krot1_pst_lapa1.Extend();
            }
        }
        void Krot1_HandRetract()
        {
            if (krot1_pst_lapa1 != null)
            {
                krot1_pst_lapa1.Velocity = speed_krot1_pst_lapa1_retract;
                krot1_pst_lapa1.Retract();
            }
        }
        // Включить огни
        void Krot1_SL_Off()
        {
            //if (a_sl)
            //{
            //    sl1.ApplyAction("OnOff_Off");
            //    sl2.ApplyAction("OnOff_Off");
            //}
        }
        // Выключить огни
        void Krot1_SL_On()
        {
            //if (a_sl)
            //{
            //    sl1.ApplyAction("OnOff_On");
            //    sl2.ApplyAction("OnOff_On");
            //}
        }
    }
}
