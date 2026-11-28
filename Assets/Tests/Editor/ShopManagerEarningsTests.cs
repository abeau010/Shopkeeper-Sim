using NUnit.Framework;
using UnityEngine;

// ShopManager counts sales toward the win target, and spending doesn't take it away.
// Run from Window > General > Test Runner > EditMode.
public class ShopManagerEarningsTests
{
    private GameObject go;
    private ShopManager shop;

    [SetUp]
    public void SetUp()
    {
        go = new GameObject("Test Shop");
        shop = go.AddComponent<ShopManager>();
    }

    [TearDown]
    public void TearDown() => Object.DestroyImmediate(go);

    [Test]
    public void StartsWithNothingEarned() => Assert.AreEqual(0, shop.TotalEarned);

    [Test]
    public void StartsAbove20PercentReputation() => Assert.Greater(shop.Reputation, 20f);

    [Test]
    public void Sales_AddUpToTotalEarned()
    {
        shop.AddMoney(40);
        shop.AddMoney(25);
        Assert.AreEqual(65, shop.TotalEarned);
        Assert.AreEqual(65, shop.Money);
    }

    [Test]
    public void Spending_LowersMoneyButNotTotalEarned()
    {
        shop.AddMoney(80);
        Assert.IsTrue(shop.TrySpendMoney(30));
        Assert.AreEqual(50, shop.Money);
        Assert.AreEqual(80, shop.TotalEarned);
    }

    [Test]
    public void FailedSpend_ChangesNothing()
    {
        shop.AddMoney(10);
        Assert.IsFalse(shop.TrySpendMoney(30));
        Assert.AreEqual(10, shop.Money);
        Assert.AreEqual(10, shop.TotalEarned);
    }

    [Test]
    public void NegativeAmount_DoesNotCountAsEarned()
    {
        shop.AddMoney(50);
        shop.AddMoney(-20);
        Assert.AreEqual(30, shop.Money);
        Assert.AreEqual(50, shop.TotalEarned);
    }

    [Test]
    public void AngryCustomers_DropReputationBelow20()
    {
        shop.RecordCustomerExit(-20); // Starts at 50
        shop.RecordCustomerExit(-15);
        Assert.Less(shop.Reputation, 20f);
    }

    [Test]
    public void Reputation_StaysBetween0And100()
    {
        shop.RecordCustomerExit(-500);
        Assert.AreEqual(0f, shop.Reputation);
        shop.RecordCustomerExit(500);
        Assert.AreEqual(100f, shop.Reputation);
    }

    [Test]
    public void StatsChanged_FiresOnSale()
    {
        bool fired = false;
        shop.OnStatsChanged += () => fired = true;
        shop.AddMoney(5);
        Assert.IsTrue(fired);
    }

    [Test]
    public void StatsChanged_FiresOnReputationChange()
    {
        bool fired = false;
        shop.OnStatsChanged += () => fired = true;
        shop.RecordCustomerExit(-5);
        Assert.IsTrue(fired);
    }

    // Wires a real ShopManager into a Round the same way GameManager does
    [Test]
    public void ShopStats_DriveRoundToWin()
    {
        var round = new Round(new RoundRules(180f, 50, 20f), shop.Reputation, shop.TotalEarned);
        shop.OnStatsChanged += () => round.UpdateStats(shop.Reputation, shop.TotalEarned);

        shop.AddMoney(30);
        shop.TrySpendMoney(25); // Buying supplies doesn't undo progress
        shop.AddMoney(21);

        Assert.AreEqual(RoundState.Won, round.State);
    }

    [Test]
    public void ShopStats_DriveRoundToLoss()
    {
        var round = new Round(new RoundRules(180f, 50, 20f), shop.Reputation, shop.TotalEarned);
        shop.OnStatsChanged += () => round.UpdateStats(shop.Reputation, shop.TotalEarned);

        shop.RecordCustomerExit(-31); // 50 -> 19

        Assert.AreEqual(RoundState.Lost, round.State);
    }
}
