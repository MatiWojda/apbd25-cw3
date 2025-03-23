using System;
using System.Collections.Generic;
using System.Linq;

// Wyjątek dla przepełnienia kontenera
public class OverfillException : Exception
{
    public OverfillException() : base("Masa ładunku przekracza maksymalną ładowność kontenera.") { }
}

// Interfejs dla powiadomień o zagrożeniach
public interface IHazardNotifier
{
    void NotifyDanger(string message);
}

// Typy produktów dla kontenerów chłodniczych
public enum ProductType
{
    Bananas,
    Chocolate,
    Fish,
    Meat,
    IceCream,
    FrozenPizza,
    Cheese,
    Sausages,
    Butter,
    Eggs
}

// Klasa pomocnicza do informacji o produktach
public static class ProductInfo
{
    public static double GetRequiredTemperature(ProductType type)
    {
        switch (type)
        {
            case ProductType.Bananas: return 13.3;
            case ProductType.Chocolate: return 18.0;
            case ProductType.Fish: return 2.0;
            case ProductType.Meat: return 4.0;
            case ProductType.IceCream: return -18.0;
            case ProductType.FrozenPizza: return -18.0;
            case ProductType.Cheese: return 7.2;
            case ProductType.Sausages: return 5.0;
            case ProductType.Butter: return 10.0;
            case ProductType.Eggs: return 19.0;
            default: throw new ArgumentException("Nieznany typ produktu");
        }
    }
}

// Klasa bazowa dla wszystkich kontenerów
public abstract class Container
{
    private static Dictionary<string, int> nextNumbers = new Dictionary<string, int>();

    public string SerialNumber { get; private set; }
    public double CargoMass { get; protected set; }
    public double Height { get; }
    public double TareWeight { get; }
    public double Depth { get; }
    public double MaxPayload { get; }

    protected Container(string typeCode, double height, double tareWeight, double depth, double maxPayload)
    {
        if (!nextNumbers.ContainsKey(typeCode))
            nextNumbers[typeCode] = 1;
        int number = nextNumbers[typeCode]++;
        SerialNumber = $"KON-{typeCode}-{number}";
        Height = height;
        TareWeight = tareWeight;
        Depth = depth;
        MaxPayload = maxPayload;
        CargoMass = 0;
    }

    public virtual void LoadCargo(double mass)
    {
        if (mass > MaxPayload)
            throw new OverfillException();
        CargoMass = mass;
    }

    public virtual void EmptyCargo()
    {
        CargoMass = 0;
    }

    public override string ToString()
    {
        return $"Kontener {SerialNumber}: Typ {GetType().Name}, Wysokość {Height} cm, Waga własna {TareWeight} kg, Głębokość {Depth} cm, Maks. ładowność {MaxPayload} kg, Masa ładunku {CargoMass} kg";
    }
}

// Kontener na płyny
public class LiquidContainer : Container, IHazardNotifier
{
    public bool IsDangerous { get; }

    public LiquidContainer(double height, double tareWeight, double depth, double maxPayload, bool isDangerous)
        : base("L", height, tareWeight, depth, maxPayload)
    {
        IsDangerous = isDangerous;
    }

    public override void LoadCargo(double mass)
    {
        double effectiveMax = IsDangerous ? 0.5 * MaxPayload : 0.9 * MaxPayload;
        if (mass > effectiveMax)
        {
            NotifyDanger($"Próba załadunku {mass} kg do kontenera {SerialNumber}, co przekracza dopuszczalne {effectiveMax} kg dla ładunku {(IsDangerous ? "niebezpiecznego" : "zwykłego")}.");
            throw new OverfillException();
        }
        base.LoadCargo(mass);
    }

    public void NotifyDanger(string message)
    {
        Console.WriteLine($"Powiadomienie o zagrożeniu dla kontenera {SerialNumber}: {message}");
    }
}

// Kontener na gaz
public class GasContainer : Container, IHazardNotifier
{
    public double Pressure { get; }

    public GasContainer(double height, double tareWeight, double depth, double maxPayload, double pressure)
        : base("G", height, tareWeight, depth, maxPayload)
    {
        Pressure = pressure;
    }

    public override void EmptyCargo()
    {
        CargoMass = 0.05 * CargoMass;
    }

    public override void LoadCargo(double mass)
    {
        if (mass > MaxPayload)
        {
            NotifyDanger($"Próba załadunku {mass} kg do kontenera {SerialNumber}, co przekracza maksymalną ładowność {MaxPayload} kg.");
            throw new OverfillException();
        }
        base.LoadCargo(mass);
    }

    public void NotifyDanger(string message)
    {
        Console.WriteLine($"Powiadomienie o zagrożeniu dla kontenera {SerialNumber}: {message}");
    }
}

// Kontener chłodniczy
public class RefrigeratedContainer : Container
{
    public ProductType ProductType { get; }
    public double MaintainedTemperature { get; }

    public RefrigeratedContainer(double height, double tareWeight, double depth, double maxPayload, ProductType productType, double maintainedTemperature)
        : base("C", height, tareWeight, depth, maxPayload)
    {
        if (maintainedTemperature < ProductInfo.GetRequiredTemperature(productType))
            throw new ArgumentException($"Temperatura utrzymywana {maintainedTemperature}°C jest niższa niż wymagana {ProductInfo.GetRequiredTemperature(productType)}°C dla {productType}.");
        ProductType = productType;
        MaintainedTemperature = maintainedTemperature;
    }
}

// Klasa reprezentująca kontenerowiec
public class ContainerShip
{
    private List<Container> containers = new List<Container>();
    public double MaxSpeed { get; }
    public int MaxContainerCount { get; }
    public double MaxWeightTons { get; }

    public ContainerShip(double maxSpeed, int maxContainerCount, double maxWeightTons)
    {
        MaxSpeed = maxSpeed;
        MaxContainerCount = maxContainerCount;
        MaxWeightTons = maxWeightTons;
    }

    public void LoadContainer(Container container)
    {
        if (containers.Count >= MaxContainerCount)
            throw new InvalidOperationException("Nie można załadować więcej kontenerów; osiągnięto maksymalną liczbę.");
        double currentWeightTons = containers.Sum(c => (c.TareWeight + c.CargoMass) / 1000.0);
        double newWeightTons = currentWeightTons + (container.TareWeight + container.CargoMass) / 1000.0;
        if (newWeightTons > MaxWeightTons)
            throw new InvalidOperationException("Nie można załadować kontenera; przekroczono by maksymalną wagę.");
        containers.Add(container);
    }

    public void LoadContainers(List<Container> newContainers)
    {
        if (containers.Count + newContainers.Count > MaxContainerCount)
            throw new InvalidOperationException("Nie można załadować kontenerów; przekroczono by maksymalną liczbę.");
        double currentWeightTons = containers.Sum(c => (c.TareWeight + c.CargoMass) / 1000.0);
        double additionalWeightTons = newContainers.Sum(c => (c.TareWeight + c.CargoMass) / 1000.0);
        if (currentWeightTons + additionalWeightTons > MaxWeightTons)
            throw new InvalidOperationException("Nie można załadować kontenerów; przekroczono by maksymalną wagę.");
        containers.AddRange(newContainers);
    }

    public void RemoveContainer(string serialNumber)
    {
        Container container = containers.FirstOrDefault(c => c.SerialNumber == serialNumber);
        if (container != null)
            containers.Remove(container);
        else
            throw new InvalidOperationException("Kontener nie został znaleziony na tym statku.");
    }

    public void ReplaceContainer(string oldSerialNumber, Container newContainer)
    {
        int index = containers.FindIndex(c => c.SerialNumber == oldSerialNumber);
        if (index == -1)
            throw new InvalidOperationException("Kontener nie został znaleziony na tym statku.");
        double currentWeightTons = containers.Sum(c => (c.TareWeight + c.CargoMass) / 1000.0);
        double weightWithoutOld = currentWeightTons - (containers[index].TareWeight + containers[index].CargoMass) / 1000.0;
        double newWeightTons = weightWithoutOld + (newContainer.TareWeight + newContainer.CargoMass) / 1000.0;
        if (newWeightTons > MaxWeightTons)
            throw new InvalidOperationException("Nie można zastąpić kontenera; przekroczono by maksymalną wagę.");
        containers[index] = newContainer;
    }

    public void PrintInfo()
    {
        Console.WriteLine($"Kontenerowiec: Maks. prędkość {MaxSpeed} węzłów, Maks. liczba kontenerów {MaxContainerCount}, Maks. waga {MaxWeightTons} ton");
        Console.WriteLine("Kontenery na pokładzie:");
        foreach (var container in containers)
            Console.WriteLine(container.ToString());
    }
}

// Przykład użycia
class Program
{
    private static List<ContainerShip> ships = new List<ContainerShip>();
    private static List<Container> containers = new List<Container>();

    static void Main()
    {
        // Tworzenie kontenerów
        containers.Add(new LiquidContainer(250, 1000, 300, 5000, true));
        containers.Add(new GasContainer(250, 1200, 300, 4000, 2.5));
        containers.Add(new RefrigeratedContainer(250, 1500, 300, 6000, ProductType.Bananas, 15.0));

        // Załadunek ładunku
        containers[0].LoadCargo(2000); // 50% z 5000 = 2500 kg
        containers[1].LoadCargo(3000);
        containers[2].LoadCargo(5000);

        // Tworzenie statku
        ships.Add(new ContainerShip(20.0, 3, 30.0));

        // Załadunek kontenerów na statek
        ships[0].LoadContainer(containers[0]);
        ships[0].LoadContainer(containers[1]);
        ships[0].LoadContainer(containers[2]);

        // Wypisanie informacji
        ships[0].PrintInfo();

        // Opróżnienie kontenera
        containers[1].EmptyCargo();
        Console.WriteLine($"Po opróżnieniu: {containers[1]}");

        // Przeniesienie kontenera (przykładowo na inny statek)
        ships.Add(new ContainerShip(18.0, 2, 25.0));
        ships[1].LoadContainer(containers[2]);
        ships[0].RemoveContainer(containers[2].SerialNumber);

        // Wypisanie zaktualizowanych informacji
        Console.WriteLine("\nPo przeniesieniu:");
        ships[0].PrintInfo();
        ships[1].PrintInfo();
        
        Console.WriteLine("Naciśnij dowolny klawisz, aby kontynuować...");
        Console.ReadKey();
        
        while (true)
        {
            Console.Clear();
            DisplayShips();
            DisplayContainers();
            DisplayMenu();
            string choice = Console.ReadLine();
            HandleChoice(choice);
            Console.WriteLine("Naciśnij dowolny klawisz, aby kontynuować...");
            Console.ReadKey();
        }
    }

    private static void DisplayShips()
    {
        Console.WriteLine("Lista kontenerowców:");
        if (ships.Count == 0)
        {
            Console.WriteLine("Brak");
        }
        else
        {
            for (int i = 0; i < ships.Count; i++)
            {
                Console.WriteLine($"{i + 1}. Statek {i + 1} (speed={ships[i].MaxSpeed}, maxContainerNum={ships[i].MaxContainerCount}, maxWeight={ships[i].MaxWeightTons})");
            }
        }
    }

    private static void DisplayContainers()
    {
        Console.WriteLine("Lista kontenerów:");
        if (containers.Count == 0)
        {
            Console.WriteLine("Brak");
        }
        else
        {
            for (int i = 0; i < containers.Count; i++)
            {
                Console.WriteLine($"{i + 1}. {containers[i].SerialNumber} - {containers[i].GetType().Name}, Masa ładunku: {containers[i].CargoMass} kg");
            }
        }
    }

    private static void DisplayMenu()
    {
        Console.WriteLine("Możliwe akcje:");
        Console.WriteLine("1. Dodaj kontenerowiec");
        Console.WriteLine("2. Usuń kontenerowiec");
        Console.WriteLine("3. Dodaj kontener");
        Console.WriteLine("4. Usuń kontener");
        Console.WriteLine("5. Umieść kontener na statku");
        Console.WriteLine("6. Usuń kontener ze statku");
        Console.WriteLine("7. Załaduj ładunek do kontenera");
        Console.WriteLine("8. Opróżnij kontener");
        Console.WriteLine("9. Wyjdź");
    }

    private static void HandleChoice(string choice)
    {
        switch (choice)
        {
            case "1":
                AddShip();
                break;
            case "2":
                RemoveShip();
                break;
            case "3":
                AddContainer();
                break;
            case "4":
                RemoveContainer();
                break;
            case "5":
                PlaceContainerOnShip();
                break;
            case "6":
                RemoveContainerFromShip();
                break;
            case "7":
                LoadCargoToContainer();
                break;
            case "8":
                EmptyContainer();
                break;
            case "9":
                Environment.Exit(0);
                break;
            default:
                Console.WriteLine("Niepoprawny wybór. Spróbuj ponownie.");
                break;
        }
    }

    private static void AddShip()
    {
        try
        {
            Console.Write("Podaj maksymalną prędkość (w węzłach): ");
            double speed = double.Parse(Console.ReadLine());
            Console.Write("Podaj maksymalną liczbę kontenerów: ");
            int maxContainerNum = int.Parse(Console.ReadLine());
            Console.Write("Podaj maksymalną wagę (w tonach): ");
            double maxWeight = double.Parse(Console.ReadLine());
            ships.Add(new ContainerShip(speed, maxContainerNum, maxWeight));
            Console.WriteLine("Kontenerowiec dodany pomyślnie.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
        }
    }

    private static void RemoveShip()
    {
        try
        {
            Console.Write("Podaj numer kontenerowca do usunięcia: ");
            int index = int.Parse(Console.ReadLine()) - 1;
            if (index >= 0 && index < ships.Count)
            {
                ships.RemoveAt(index);
                Console.WriteLine("Kontenerowiec usunięty pomyślnie.");
            }
            else
            {
                Console.WriteLine("Niepoprawny numer.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
        }
    }

    private static void AddContainer()
    {
        try
        {
            Console.WriteLine("Wybierz typ kontenera:");
            Console.WriteLine("1. Płyn");
            Console.WriteLine("2. Gaz");
            Console.WriteLine("3. Chłodniczy");
            string type = Console.ReadLine();

            Console.Write("Podaj wysokość (cm): ");
            double height = double.Parse(Console.ReadLine());
            Console.Write("Podaj wagę własną (kg): ");
            double tareWeight = double.Parse(Console.ReadLine());
            Console.Write("Podaj głębokość (cm): ");
            double depth = double.Parse(Console.ReadLine());
            Console.Write("Podaj maksymalną ładowność (kg): ");
            double maxPayload = double.Parse(Console.ReadLine());

            switch (type)
            {
                case "1":
                    Console.Write("Czy jest niebezpieczny (tak/nie): ");
                    bool isDangerous = Console.ReadLine().ToLower() == "tak";
                    containers.Add(new LiquidContainer(height, tareWeight, depth, maxPayload, isDangerous));
                    Console.WriteLine("Kontener na płyny dodany pomyślnie.");
                    break;
                case "2":
                    Console.Write("Podaj ciśnienie (atm): ");
                    double pressure = double.Parse(Console.ReadLine());
                    containers.Add(new GasContainer(height, tareWeight, depth, maxPayload, pressure));
                    Console.WriteLine("Kontener na gaz dodany pomyślnie.");
                    break;
                case "3":
                    Console.WriteLine("Wybierz typ produktu:");
                    foreach (ProductType pt in Enum.GetValues(typeof(ProductType)))
                    {
                        Console.WriteLine($"{(int)pt + 1}. {pt}");
                    }
                    int productChoice = int.Parse(Console.ReadLine()) - 1;
                    ProductType productType = (ProductType)productChoice;
                    Console.Write("Podaj utrzymywaną temperaturę (°C): ");
                    double temperature = double.Parse(Console.ReadLine());
                    containers.Add(new RefrigeratedContainer(height, tareWeight, depth, maxPayload, productType, temperature));
                    Console.WriteLine("Kontener chłodniczy dodany pomyślnie.");
                    break;
                default:
                    Console.WriteLine("Niepoprawny typ.");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
        }
    }

    private static void RemoveContainer()
    {
        try
        {
            Console.Write("Podaj numer kontenera do usunięcia: ");
            int index = int.Parse(Console.ReadLine()) - 1;
            if (index >= 0 && index < containers.Count)
            {
                containers.RemoveAt(index);
                Console.WriteLine("Kontener usunięty pomyślnie.");
            }
            else
            {
                Console.WriteLine("Niepoprawny numer.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
        }
    }

    private static void PlaceContainerOnShip()
    {
        try
        {
            Console.Write("Podaj numer kontenerowca: ");
            int shipIndex = int.Parse(Console.ReadLine()) - 1;
            Console.Write("Podaj numer kontenera: ");
            int containerIndex = int.Parse(Console.ReadLine()) - 1;
            if (shipIndex >= 0 && shipIndex < ships.Count && containerIndex >= 0 && containerIndex < containers.Count)
            {
                ships[shipIndex].LoadContainer(containers[containerIndex]);
                Console.WriteLine("Kontener umieszczony na statku pomyślnie.");
            }
            else
            {
                Console.WriteLine("Niepoprawny numer.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
        }
    }

    private static void RemoveContainerFromShip()
    {
        try
        {
            Console.Write("Podaj numer kontenerowca: ");
            int shipIndex = int.Parse(Console.ReadLine()) - 1;
            Console.Write("Podaj numer seryjny kontenera: ");
            string serialNumber = Console.ReadLine();
            if (shipIndex >= 0 && shipIndex < ships.Count)
            {
                ships[shipIndex].RemoveContainer(serialNumber);
                Console.WriteLine("Kontener usunięty ze statku pomyślnie.");
            }
            else
            {
                Console.WriteLine("Niepoprawny numer.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
        }
    }

    private static void LoadCargoToContainer()
    {
        try
        {
            Console.Write("Podaj numer kontenera: ");
            int index = int.Parse(Console.ReadLine()) - 1;
            if (index >= 0 && index < containers.Count)
            {
                Console.Write("Podaj masę ładunku (kg): ");
                double mass = double.Parse(Console.ReadLine());
                containers[index].LoadCargo(mass);
                Console.WriteLine("Ładunek załadowany pomyślnie.");
            }
            else
            {
                Console.WriteLine("Niepoprawny numer.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
        }
    }

    private static void EmptyContainer()
    {
        try
        {
            Console.Write("Podaj numer kontenera: ");
            int index = int.Parse(Console.ReadLine()) - 1;
            if (index >= 0 && index < containers.Count)
            {
                containers[index].EmptyCargo();
                Console.WriteLine("Kontener opróżniony pomyślnie.");
            }
            else
            {
                Console.WriteLine("Niepoprawny numer.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
        }
    }
}