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
    
    public List<Container> GetContainers()
    {
        return new List<Container>(containers); // Zwraca kopię listy kontenerów
    }

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
        ships.Add(new ContainerShip(20.0, 5, 30.0));

        // Załadunek kontenerów na statek i usuwanie z listy containers
        ships[0].LoadContainer(containers[0]);
        containers.Remove(containers[0]);

        ships[0].LoadContainer(containers[0]); // Teraz indeks 0 to dawny containers[1]
        containers.Remove(containers[0]);

        ships[0].LoadContainer(containers[0]); // Teraz indeks 0 to dawny containers[2]
        containers.Remove(containers[0]);

        // Wypisanie informacji
        ships[0].PrintInfo();

        // Opróżnienie kontenera
        // Kontener jest na statku, więc odwołujemy się do niego przez statek
        var containerToEmpty = ships[0].GetContainers().First(c => c.SerialNumber == "KON-L-1"); // Przykładowy numer seryjny
        containerToEmpty.EmptyCargo();
        Console.WriteLine($"Po opróżnieniu: {containerToEmpty}");

        // Przeniesienie kontenera na inny statek
        ships.Add(new ContainerShip(18.0, 2, 25.0));

        // Usuwamy kontener z pierwotnego statku i dodajemy do containers
        var containerToMove = ships[0].GetContainers().First(c => c.SerialNumber == "KON-C-1"); // Przykładowy numer seryjny
        ships[0].RemoveContainer(containerToMove.SerialNumber);
        containers.Add(containerToMove); // Dodajemy z powrotem do listy containers

        // Ładujemy kontener na nowy statek i usuwamy z containers
        ships[1].LoadContainer(containerToMove);
        containers.Remove(containerToMove); // Usuwamy z listy po załadowaniu

        // Wypisanie zaktualizowanych informacji
        Console.WriteLine("\nPo przeniesieniu:");
        ships[0].PrintInfo();
        ships[1].PrintInfo();

        Console.WriteLine("Naciśnij dowolny klawisz, aby kontynuować...");
        Console.ReadKey();
        
        // Przykład działania
        while (true)
        {
            Console.Clear();
            DisplayMainMenu();
            string choice = Console.ReadLine();
            switch (choice)
            {
                case "1": AddShip(); break;
                case "2": RemoveShip(); break;
                case "3": AddContainer(); break;
                case "4": RemoveContainer(); break;
                case "5": LoadContainerToShip(); break;
                case "6": RemoveContainerFromShip(); break;
                case "7": LoadCargoToContainer(); break;
                case "8": EmptyContainer(); break;
                case "9": ReplaceContainerOnShip(); break;
                case "10": DisplayShipInfo(); break;
                case "11": DisplayContainerInfo(); break;
                case "12": return;
                default:
                    Console.WriteLine("Nieprawidłowy wybór. Naciśnij Enter, aby kontynuować.");
                    Console.ReadLine();
                    break;
            }
        }
    }

    static void DisplayMainMenu()
    {
        Console.WriteLine("Lista kontenerowców:");
        if (ships.Count == 0)
            Console.WriteLine("Brak");
        else
            for (int i = 0; i < ships.Count; i++)
                Console.WriteLine($"{i + 1}. Statek {i + 1} (speed={ships[i].MaxSpeed}, maxContainerNum={ships[i].MaxContainerCount}, maxWeight={ships[i].MaxWeightTons})");

        Console.WriteLine("\nLista kontenerów w magazynie:");
        if (containers.Count == 0)
            Console.WriteLine("Brak");
        else
            for (int i = 0; i < containers.Count; i++)
                Console.WriteLine($"{i + 1}. {containers[i].SerialNumber} - {containers[i].GetType().Name}");

        Console.WriteLine("\nMożliwe akcje:");
        Console.WriteLine("1. Dodaj kontenerowiec");
        Console.WriteLine("2. Usuń kontenerowiec");
        Console.WriteLine("3. Dodaj kontener");
        Console.WriteLine("4. Usuń kontener");
        Console.WriteLine("5. Załaduj kontener na statek");
        Console.WriteLine("6. Usuń kontener ze statku");
        Console.WriteLine("7. Załaduj ładunek do kontenera");
        Console.WriteLine("8. Opróżnij kontener");
        Console.WriteLine("9. Zastąp kontener na statku");
        Console.WriteLine("10. Wyświetl informacje o kontenerowcu i załadowanych na niego kontenerach");
        Console.WriteLine("11. Wyświetl informacje o kontenerze");
        Console.WriteLine("12. Zakończ");
        Console.Write("Wybierz akcję: ");
    }

    static void AddShip()
    {
        try
        {
            Console.Write("Podaj maksymalną prędkość (węzły): ");
            double maxSpeed = double.Parse(Console.ReadLine());
            Console.Write("Podaj maksymalną liczbę kontenerów: ");
            int maxContainerCount = int.Parse(Console.ReadLine());
            Console.Write("Podaj maksymalną wagę (tony): ");
            double maxWeightTons = double.Parse(Console.ReadLine());
            ships.Add(new ContainerShip(maxSpeed, maxContainerCount, maxWeightTons));
            Console.WriteLine("Kontenerowiec dodany. Naciśnij Enter, aby kontynuować.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message} Naciśnij Enter, aby kontynuować.");
        }
        Console.ReadLine();
    }

    static void RemoveShip()
    {
        if (ships.Count == 0)
        {
            Console.WriteLine("Brak kontenerowców do usunięcia. Naciśnij Enter, aby kontynuować.");
            Console.ReadLine();
            return;
        }
        Console.WriteLine("Wybierz numer kontenerowca do usunięcia:");
        for (int i = 0; i < ships.Count; i++)
            Console.WriteLine($"{i + 1}. Statek {i + 1}");
        try
        {
            int choice = int.Parse(Console.ReadLine()) - 1;
            if (choice >= 0 && choice < ships.Count)
            {
                ships.RemoveAt(choice);
                Console.WriteLine("Kontenerowiec usunięty. Naciśnij Enter, aby kontynuować.");
            }
            else
            {
                Console.WriteLine("Nieprawidłowy wybór. Naciśnij Enter, aby kontynuować.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message} Naciśnij Enter, aby kontynuować.");
        }
        Console.ReadLine();
    }

    static void AddContainer()
    {
        try
        {
            Console.WriteLine("Wybierz typ kontenera:");
            Console.WriteLine("1. Płynny");
            Console.WriteLine("2. Gazowy");
            Console.WriteLine("3. Chłodniczy");
            string typeChoice = Console.ReadLine();

            Console.Write("Podaj wysokość (cm): ");
            double height = double.Parse(Console.ReadLine());
            Console.Write("Podaj wagę własną (kg): ");
            double tareWeight = double.Parse(Console.ReadLine());
            Console.Write("Podaj głębokość (cm): ");
            double depth = double.Parse(Console.ReadLine());
            Console.Write("Podaj maksymalną ładowność (kg): ");
            double maxPayload = double.Parse(Console.ReadLine());

            switch (typeChoice)
            {
                case "1":
                    Console.Write("Czy kontener jest na ładunek niebezpieczny? (tak/nie): ");
                    bool isDangerous = Console.ReadLine().ToLower() == "tak";
                    containers.Add(new LiquidContainer(height, tareWeight, depth, maxPayload, isDangerous));
                    break;
                case "2":
                    Console.Write("Podaj ciśnienie (atm): ");
                    double pressure = double.Parse(Console.ReadLine());
                    containers.Add(new GasContainer(height, tareWeight, depth, maxPayload, pressure));
                    break;
                case "3":
                    Console.WriteLine("Wybierz typ produktu:");
                    foreach (ProductType type in Enum.GetValues(typeof(ProductType)))
                        Console.WriteLine($"{(int)type + 1}. {type}");
                    int productChoice = int.Parse(Console.ReadLine()) - 1;
                    ProductType productType = (ProductType)productChoice;
                    Console.Write("Podaj utrzymywaną temperaturę (°C): ");
                    double maintainedTemperature = double.Parse(Console.ReadLine());
                    containers.Add(new RefrigeratedContainer(height, tareWeight, depth, maxPayload, productType, maintainedTemperature));
                    break;
                default:
                    Console.WriteLine("Nieprawidłowy wybór typu. Naciśnij Enter, aby kontynuować.");
                    Console.ReadLine();
                    return;
            }
            Console.WriteLine("Kontener dodany. Naciśnij Enter, aby kontynuować.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message} Naciśnij Enter, aby kontynuować.");
        }
        Console.ReadLine();
    }

    static void RemoveContainer()
    {
        if (containers.Count == 0)
        {
            Console.WriteLine("Brak kontenerów do usunięcia. Naciśnij Enter, aby kontynuować.");
            Console.ReadLine();
            return;
        }
        Console.WriteLine("Wybierz numer kontenera do usunięcia:");
        for (int i = 0; i < containers.Count; i++)
            Console.WriteLine($"{i + 1}. {containers[i].SerialNumber}");
        try
        {
            int choice = int.Parse(Console.ReadLine()) - 1;
            if (choice >= 0 && choice < containers.Count)
            {
                containers.RemoveAt(choice);
                Console.WriteLine("Kontener usunięty. Naciśnij Enter, aby kontynuować.");
            }
            else
            {
                Console.WriteLine("Nieprawidłowy wybór. Naciśnij Enter, aby kontynuować.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message} Naciśnij Enter, aby kontynuować.");
        }
        Console.ReadLine();
    }

    static void LoadContainerToShip()
    {
        if (containers.Count == 0 || ships.Count == 0)
        {
            Console.WriteLine("Brak kontenerów lub kontenerowców. Naciśnij Enter, aby kontynuować.");
            Console.ReadLine();
            return;
        }
        Console.WriteLine("Wybierz numer kontenera do załadowania:");
        for (int i = 0; i < containers.Count; i++)
            Console.WriteLine($"{i + 1}. {containers[i].SerialNumber}");
        try
        {
            int containerChoice = int.Parse(Console.ReadLine()) - 1;
            if (containerChoice < 0 || containerChoice >= containers.Count)
            {
                Console.WriteLine("Nieprawidłowy wybór kontenera. Naciśnij Enter, aby kontynuować.");
                Console.ReadLine();
                return;
            }
            Console.WriteLine("Wybierz numer kontenerowca:");
            for (int i = 0; i < ships.Count; i++)
                Console.WriteLine($"{i + 1}. Statek {i + 1}");
            int shipChoice = int.Parse(Console.ReadLine()) - 1;
            if (shipChoice < 0 || shipChoice >= ships.Count)
            {
                Console.WriteLine("Nieprawidłowy wybór statku. Naciśnij Enter, aby kontynuować.");
                Console.ReadLine();
                return;
            }
            Container containerToLoad = containers[containerChoice];
            ships[shipChoice].LoadContainer(containerToLoad);
            containers.RemoveAt(containerChoice); // Usuwamy kontener z listy dostępnych
            Console.WriteLine("Kontener załadowany na statek i usunięty z listy dostępnych. Naciśnij Enter, aby kontynuować.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message} Naciśnij Enter, aby kontynuować.");
        }
        Console.ReadLine();
    }

    static void RemoveContainerFromShip()
    {
        if (ships.Count == 0)
        {
            Console.WriteLine("Brak kontenerowców. Naciśnij Enter, aby kontynuować.");
            Console.ReadLine();
            return;
        }
        Console.WriteLine("Wybierz numer kontenerowca:");
        for (int i = 0; i < ships.Count; i++)
            Console.WriteLine($"{i + 1}. Statek {i + 1}");
        try
        {
            int shipChoice = int.Parse(Console.ReadLine()) - 1;
            if (shipChoice < 0 || shipChoice >= ships.Count)
            {
                Console.WriteLine("Nieprawidłowy wybór statku. Naciśnij Enter, aby kontynuować.");
                Console.ReadLine();
                return;
            }
            var shipContainers = ships[shipChoice].GetContainers();
            if (shipContainers.Count == 0)
            {
                Console.WriteLine("Brak kontenerów na tym statku. Naciśnij Enter, aby kontynuować.");
                Console.ReadLine();
                return;
            }
            Console.WriteLine("Wybierz numer kontenera do usunięcia:");
            for (int i = 0; i < shipContainers.Count; i++)
                Console.WriteLine($"{i + 1}. {shipContainers[i].SerialNumber}");
            int containerChoice = int.Parse(Console.ReadLine()) - 1;
            if (containerChoice >= 0 && containerChoice < shipContainers.Count)
            {
                Container containerToRemove = shipContainers[containerChoice];
                ships[shipChoice].RemoveContainer(containerToRemove.SerialNumber);
                containers.Add(containerToRemove); // Dodajemy kontener z powrotem do listy dostępnych
                Console.WriteLine("Kontener usunięty ze statku i dodany do listy dostępnych. Naciśnij Enter, aby kontynuować.");
            }
            else
            {
                Console.WriteLine("Nieprawidłowy wybór. Naciśnij Enter, aby kontynuować.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message} Naciśnij Enter, aby kontynuować.");
        }
        Console.ReadLine();
    }
    
    static void LoadCargoToContainer()
    {
        if (containers.Count == 0)
        {
            Console.WriteLine("Brak kontenerów. Naciśnij Enter, aby kontynuować.");
            Console.ReadLine();
            return;
        }

        Console.WriteLine("Wybierz numer kontenera do załadowania ładunku:");
        for (int i = 0; i < containers.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {containers[i].SerialNumber} - {containers[i].GetType().Name}");
        }

        try
        {
            int choice = int.Parse(Console.ReadLine()) - 1;
            if (choice >= 0 && choice < containers.Count)
            {
                Console.Write("Podaj masę ładunku (kg): ");
                double cargoMass = double.Parse(Console.ReadLine());
                containers[choice].LoadCargo(cargoMass);
                Console.WriteLine("Ładunek załadowany pomyślnie.");
            }
            else
            {
                Console.WriteLine("Nieprawidłowy wybór.");
            }
        }
        catch (OverfillException ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
        }

        Console.WriteLine("Naciśnij Enter, aby kontynuować.");
        Console.ReadLine();
    }

    static void EmptyContainer()
    {
        if (containers.Count == 0)
        {
            Console.WriteLine("Brak kontenerów do opróżnienia. Naciśnij Enter, aby kontynuować.");
            Console.ReadLine();
            return;
        }
        Console.WriteLine("Wybierz numer kontenera do opróżnienia:");
        for (int i = 0; i < containers.Count; i++)
            Console.WriteLine($"{i + 1}. {containers[i].SerialNumber}");
        try
        {
            int choice = int.Parse(Console.ReadLine()) - 1;
            if (choice >= 0 && choice < containers.Count)
            {
                containers[choice].EmptyCargo();
                Console.WriteLine("Kontener opróżniony. Naciśnij Enter, aby kontynuować.");
            }
            else
            {
                Console.WriteLine("Nieprawidłowy wybór. Naciśnij Enter, aby kontynuować.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message} Naciśnij Enter, aby kontynuować.");
        }
        Console.ReadLine();
    }

    static void ReplaceContainerOnShip()
    {
        if (ships.Count == 0 || containers.Count == 0)
        {
            Console.WriteLine("Brak kontenerowców lub kontenerów. Naciśnij Enter, aby kontynuować.");
            Console.ReadLine();
            return;
        }
        Console.WriteLine("Wybierz numer kontenerowca:");
        for (int i = 0; i < ships.Count; i++)
            Console.WriteLine($"{i + 1}. Statek {i + 1}");
        try
        {
            int shipChoice = int.Parse(Console.ReadLine()) - 1;
            if (shipChoice < 0 || shipChoice >= ships.Count)
            {
                Console.WriteLine("Nieprawidłowy wybór statku. Naciśnij Enter, aby kontynuować.");
                Console.ReadLine();
                return;
            }
            var shipContainers = ships[shipChoice].GetContainers();
            if (shipContainers.Count == 0)
            {
                Console.WriteLine("Brak kontenerów na tym statku. Naciśnij Enter, aby kontynuować.");
                Console.ReadLine();
                return;
            }
            Console.WriteLine("Wybierz numer kontenera do zastąpienia:");
            for (int i = 0; i < shipContainers.Count; i++)
                Console.WriteLine($"{i + 1}. {shipContainers[i].SerialNumber}");
            int oldContainerChoice = int.Parse(Console.ReadLine()) - 1;
            if (oldContainerChoice < 0 || oldContainerChoice >= shipContainers.Count)
            {
                Console.WriteLine("Nieprawidłowy wybór kontenera. Naciśnij Enter, aby kontynuować.");
                Console.ReadLine();
                return;
            }
            Console.WriteLine("Wybierz numer nowego kontenera:");
            for (int i = 0; i < containers.Count; i++)
                Console.WriteLine($"{i + 1}. {containers[i].SerialNumber}");
            int newContainerChoice = int.Parse(Console.ReadLine()) - 1;
            if (newContainerChoice < 0 || newContainerChoice >= containers.Count)
            {
                Console.WriteLine("Nieprawidłowy wybór nowego kontenera. Naciśnij Enter, aby kontynuować.");
                Console.ReadLine();
                return;
            }
            containers.Add(shipContainers[oldContainerChoice]);
            ships[shipChoice].ReplaceContainer(shipContainers[oldContainerChoice].SerialNumber, containers[newContainerChoice]);
            containers.Remove(containers[newContainerChoice]);
            Console.WriteLine("Kontener zastąpiony. Naciśnij Enter, aby kontynuować.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message} Naciśnij Enter, aby kontynuować.");
        }
        Console.ReadLine();
    }

    static void DisplayShipInfo()
    {
        if (ships.Count == 0)
        {
            Console.WriteLine("Brak kontenerowców. Naciśnij Enter, aby kontynuować.");
            Console.ReadLine();
            return;
        }
        Console.WriteLine("Wybierz numer kontenerowca do wyświetlenia informacji:");
        for (int i = 0; i < ships.Count; i++)
            Console.WriteLine($"{i + 1}. Statek {i + 1}");
        try
        {
            int choice = int.Parse(Console.ReadLine()) - 1;
            if (choice >= 0 && choice < ships.Count)
            {
                ships[choice].PrintInfo();
            }
            else
            {
                Console.WriteLine("Nieprawidłowy wybór.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
        }
        Console.WriteLine("Naciśnij Enter, aby kontynuować.");
        Console.ReadLine();
    }

    static void DisplayContainerInfo()
    {
        if (containers.Count == 0)
        {
            Console.WriteLine("Brak kontenerów w magazynie. Naciśnij Enter, aby kontynuować.");
            Console.ReadLine();
            return;
        }
        Console.WriteLine("Wybierz numer kontenera do wyświetlenia informacji:");
        for (int i = 0; i < containers.Count; i++)
            Console.WriteLine($"{i + 1}. {containers[i].SerialNumber}");
        try
        {
            int choice = int.Parse(Console.ReadLine()) - 1;
            if (choice >= 0 && choice < containers.Count)
            {
                Console.WriteLine(containers[choice].ToString());
            }
            else
            {
                Console.WriteLine("Nieprawidłowy wybór.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd: {ex.Message}");
        }
        Console.WriteLine("Naciśnij Enter, aby kontynuować.");
        Console.ReadLine();
    }
}