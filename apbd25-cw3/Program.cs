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
    static void Main()
    {
        // Tworzenie kontenerów
        var liquidContainer = new LiquidContainer(250, 1000, 300, 5000, true);
        var gasContainer = new GasContainer(250, 1200, 300, 4000, 2.5);
        var refrigeratedContainer = new RefrigeratedContainer(250, 1500, 300, 6000, ProductType.Bananas, 15.0);

        // Załadunek ładunku
        liquidContainer.LoadCargo(2000); // 50% z 5000 = 2500 kg
        gasContainer.LoadCargo(3000);
        refrigeratedContainer.LoadCargo(5000);

        // Tworzenie statku
        var ship = new ContainerShip(20.0, 3, 30.0);

        // Załadunek kontenerów na statek
        ship.LoadContainer(liquidContainer);
        ship.LoadContainer(gasContainer);
        ship.LoadContainer(refrigeratedContainer);

        // Wypisanie informacji
        ship.PrintInfo();

        // Opróżnienie kontenera
        gasContainer.EmptyCargo();
        Console.WriteLine($"Po opróżnieniu: {gasContainer}");

        // Przeniesienie kontenera (przykładowo na inny statek)
        var ship2 = new ContainerShip(18.0, 2, 25.0);
        ship2.LoadContainer(refrigeratedContainer);
        ship.RemoveContainer(refrigeratedContainer.SerialNumber);

        // Wypisanie zaktualizowanych informacji
        Console.WriteLine("\nPo przeniesieniu:");
        ship.PrintInfo();
        ship2.PrintInfo();
    }
}