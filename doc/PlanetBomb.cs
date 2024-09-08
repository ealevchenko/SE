    //Объявляем переменные
    // ------------------------------------------

    double DropHeight=20000; //Высота сброса бомбы

    IMyCameraBlock Camera; //Объявляем блоки
    IMyTextPanel LCD;

    Vector3D PlanetXYZ; //Координаты центра планеты
    Vector3D BaseXYZ; //Координаты базы
    Vector3D DropPointXYZ; //Здесь будут координаты точки сброса

	
    //Конструктор скрипта
    // ------------------------------------------
	
    public Program()
    {
        //Находим блоки
        Camera = GridTerminalSystem.GetBlockWithName("Camera") as IMyCameraBlock;
        Camera.EnableRaycast = true;
        LCD = GridTerminalSystem.GetBlockWithName("LCD") as IMyTextPanel;
        //Создаем пустые вектора
        PlanetXYZ = new Vector3D();
        BaseXYZ = new Vector3D();
        DropPointXYZ = new Vector3D();
    }

	
    //Главная функция
    // ------------------------------------------
	
    public void Main(string arg)
    {
        //Разбор аргументов. Вызов функций raycast и расчета точки сброса.
        if (arg == "Detect") {
            Detect();
        }
        else if (arg == "Calculate")
        {
            CalculateDropPoint();
        }

    }

	
    //Мои процедуры
    // ------------------------------------------

    // Рейкаст и установка координат центра планеты и базы
    void Detect()
    {
        MyDetectedEntityInfo DetectedObject = Camera.Raycast(10000, 0, 0);
        LCD.WritePublicText("Обнаружено: \n", false);
        LCD.WritePublicText("Объект: " + DetectedObject.Name + "\n", true);
        LCD.WritePublicText("Координаты: \n", true);
        LCD.WritePublicText("     X: " + DetectedObject.Position.X + "\n", true);
        LCD.WritePublicText("     Y: " + DetectedObject.Position.Y + "\n", true);
        LCD.WritePublicText("     Z: " + DetectedObject.Position.Z + "\n", true);

        string GPS = "\nGPS:" + DetectedObject.Name + ":" + DetectedObject.Position.X + ":"
                              + DetectedObject.Position.Y + ":"
                              + DetectedObject.Position.Z + ":";
        LCD.WritePublicText(GPS, true);
        //Если обнаруженный объект - планета, устанавливаем PlanetXYZ
        if (DetectedObject.Type == MyDetectedEntityType.Planet)
        {
            PlanetXYZ = DetectedObject.Position;
        }
        //Если обнаруженный объект - большой грид, устанавливаем BaseXYZ
        else if (DetectedObject.Type == MyDetectedEntityType.LargeGrid)
        {
            BaseXYZ = DetectedObject.Position;
        }
    }

    //Расчет точки сброса
    void CalculateDropPoint()
    {
        //Вектор вертикали, проходящий через базу
        Vector3D VerticalVector = BaseXYZ - PlanetXYZ;
        //Нормализация вертикали
        Vector3D VerticalNorm = Vector3D.Normalize(VerticalVector);
        //Расчет точки сброса
        DropPointXYZ = BaseXYZ + VerticalNorm * DropHeight;
        //Создаем GPS-метку
        string GPS = "GPS:DropPoint:" + DropPointXYZ.X + ":" + DropPointXYZ.Y + ":" + DropPointXYZ.Z + ":";
        LCD.WritePublicText("Точка сброса:\n" + GPS, false);
    }

