using NUnit.Framework;

// How the difficulty's spawn rate shortens the gap between customers.
// Run from Window > General > Test Runner > EditMode.
public class SpawnRateTests
{
    [Test]
    public void NormalRate_KeepsDelay() => Assert.AreEqual(9f, CustomerSpawner.ScaleDelay(9f, 1f));

    [Test]
    public void TripleRate_CutsDelayToAThird() => Assert.AreEqual(3f, CustomerSpawner.ScaleDelay(9f, 3f), 0.0001f);

    [Test]
    public void Hard_DefaultGapIs3xShorterThanEasy()
    {
        // The spawner waits 5-10s between customers by default
        float easyMin = CustomerSpawner.ScaleDelay(5f, Difficulty.Easy.SpawnRateMultiplier);
        float easyMax = CustomerSpawner.ScaleDelay(10f, Difficulty.Easy.SpawnRateMultiplier);
        float hardMin = CustomerSpawner.ScaleDelay(5f, Difficulty.Hard.SpawnRateMultiplier);
        float hardMax = CustomerSpawner.ScaleDelay(10f, Difficulty.Hard.SpawnRateMultiplier);

        Assert.AreEqual(easyMin / 3f, hardMin, 0.0001f);
        Assert.AreEqual(easyMax / 3f, hardMax, 0.0001f);
    }

    [TestCase(0f)]
    [TestCase(-2f)]
    public void InvalidRate_KeepsDelay(float multiplier) =>
        Assert.AreEqual(9f, CustomerSpawner.ScaleDelay(9f, multiplier));
}

// How many customers the spawner lets into the shop at once
public class SpawnerMaxCustomersTests
{
    private UnityEngine.GameObject go;
    private CustomerSpawner spawner;

    [SetUp]
    public void SetUp()
    {
        go = new UnityEngine.GameObject("Test Spawner");
        spawner = go.AddComponent<CustomerSpawner>();
    }

    [TearDown]
    public void TearDown() => UnityEngine.Object.DestroyImmediate(go);

    [Test]
    public void Default_UsesLevelLimitOfOne() => Assert.AreEqual(1, spawner.MaxCustomers);

    [Test]
    public void EasySetting_OneAtATime()
    {
        spawner.SetMaxCustomers(Difficulty.Easy.MaxCustomers);
        Assert.AreEqual(1, spawner.MaxCustomers);
    }

    [Test]
    public void HardSetting_AllowsThreeAtOnce()
    {
        spawner.SetMaxCustomers(Difficulty.Hard.MaxCustomers);
        Assert.AreEqual(3, spawner.MaxCustomers);
    }

    [Test]
    public void ZeroOrLess_GoesBackToLevelLimit()
    {
        spawner.SetMaxCustomers(3);
        spawner.SetMaxCustomers(0);
        Assert.AreEqual(1, spawner.MaxCustomers);
    }
}
