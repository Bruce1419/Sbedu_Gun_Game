using EFT;
using EFT.InventoryLogic;
using Comfort.Common;
using BepInEx;
using System.Collections.Generic;
using BepInEx.Configuration;

namespace Sbedu_Gun_Game
{
    [BepInPlugin("com.Sbedu_Gun_Game", "Sbedu_Gun_Game", "1.1.0")]
    public class SbeduGunGame : BaseUnityPlugin
    {
        private ConfigEntry<bool> configIsSwapEnable;
        private ConfigEntry<bool> configIsReloadEnable;
        private static GameWorld gameWorld;
        public static Player gamePlayer = null;
        public static List<int> listAiID = new List<int>();
        public static bool doneTest = false;
        private void Awake()
        {
            Logger.LogInfo("Sbedu Gun Game is loading!...");
            configIsSwapEnable = Config.Bind("General",      // The section under which the option is shown
                                         "Swap Weapon",  // The key of the configuration option in the configuration file
                                         true, // The default value
                                         "After every kill, the weapon of the killed enemy is taken.");
            configIsReloadEnable = Config.Bind("General",      // The section under which the option is shown
                                         "Reload weapon",  // The key of the configuration option in the configuration file
                                         false, // The default value
                                         "After every kill, all the ammunition in the magazine is reloaded.");
            Logger.LogInfo("Sbedu Gun Game has loaded!");
        }

        private void Update()
        {
            if (configIsReloadEnable.Value || configIsSwapEnable.Value)
                CheckAIDeath();
        }

        bool Ready()
        {               
            if (gameWorld == null || gameWorld.AllPlayers == null || gameWorld.AllPlayers.Count <= 0) 
                return false;
            else 
                return true;
        }

        public void CheckAIDeath()
        {
            gameWorld = Singleton<GameWorld>.Instance;
            if (!Ready())
            {
                return;
            }
            var playerList = gameWorld.AllPlayers;
            var gamePlayer = playerList[0];
            if (playerList.Count > 1)
            {
                foreach (var item in playerList)
                {
                    try
                    {
                        if (!listAiID.Contains(item.Id) && item.Id != gamePlayer.Id)
                        {
                            //Logger.LogWarning("Adding AI ID: " + item.Id);
                            item.OnPlayerDead += (Player killedGuy, Player killer, DamageInfo damageInfo, EBodyPart part) =>
                            {
                                //Logger.LogWarning("Killed AI: " + killedGuy.Id);
                                if (killer.Id == gamePlayer.Id)
                                {
                                    bool swapped = false;
                                    //Logger.LogWarning("Taking weapon of: " + item.Id);
                                    if (configIsSwapEnable.Value && killedGuy.HandsController.Item is Weapon)
                                    {
                                        bool reloaded = false;
                                        if (configIsReloadEnable.Value)
                                        {
                                            try
                                            {
                                                var ammo = TakeChamberAmmo((Weapon)item.HandsController.Item);
                                                if (ammo != null) //Check if the weapon has at least 1 ammo
                                                {
                                                    ReloadWeapon((Weapon)item.HandsController.Item);
                                                    //Logger.LogWarning("Realoded enemy weapon!");
                                                }
                                            }
                                            catch (System.Exception e) { Logger.LogWarning("Error reloading enemy weapon: " + e); }
                                        }
                                        try
                                        {
                                            if (reloaded)
                                            {
                                                gamePlayer.SetInHands((Weapon)item.HandsController.Item, null);
                                                swapped = true;
                                            }
                                        } //Take enemy weapon
                                        catch (System.Exception e) { Logger.LogWarning("Error swap: " + e); }
                                    }
                                    if (configIsReloadEnable.Value && !swapped)
                                    {
                                        try
                                        {
                                            if (gamePlayer.HandsController.Item is Weapon)
                                            {
                                                var ammo = TakeChamberAmmo((Weapon)gamePlayer.HandsController.Item);
                                                if (ammo != null) //Check if the weapon has at least 1 ammo
                                                {
                                                    ReloadWeapon((Weapon)gamePlayer.HandsController.Item);
                                                    //Logger.LogWarning("PLayer weapon reloaded!");
                                                }
                                            }
                                        }
                                        catch (System.Exception e) { Logger.LogWarning("Error reloading player weapon: " + e); }
                                    }
                                }
                                //Logger.LogWarning("Removing: " + killedGuy.Id);
                                listAiID.Remove(item.Id);
                            };
                            listAiID.Add(item.Id);
                        }
                    }
                    catch (System.Exception e){ Logger.LogWarning("Error getting enemy ID: "+e); }
                }
            }
            else
                listAiID.Clear();
        }

        //public void Test()
        //{
        //    gameWorld = Singleton<GameWorld>.Instance;
        //    if (!Ready())
        //    {
        //        return;
        //    }
        //    var playerList = gameWorld.AllPlayers;
        //    var gamePlayer = playerList[0];
        //    if (doneTest == false)
        //    {
        //        Logger.LogWarning("Creo l'evento!");
        //        gamePlayer.HandsChangedEvent += (GInterface98 interface98) =>
        //        {
        //            
        //        };
        //        doneTest = true;
        //    }
        //}

        private Item TakeChamberAmmo(Weapon weapon) //Take 1 ammo from a weapon
        {
            Item ammo = null;
            foreach (var chamber in weapon.Chambers)
            {
                foreach (var item in chamber.Items)
                {
                    ammo = item;
                    //Logger.LogWarning("Preso proiettile dalla camera: " + item.LocalizedName());
                    break;
                }
            } //Works with normal magazine
            if (ammo == null)
            {
                if (weapon.GetCurrentMagazine() != null)
                {
                    ammo = weapon.GetCurrentMagazine().FirstRealAmmo();
                    //Logger.LogWarning("Preso primo proiettile dal caricatore: " + ammo.LocalizedName());
                }
                    

            } //Works with round magazine
            return ammo;
        }

        private void ReloadWeapon(Weapon weapon) //Reload weapon method
        {
            try
            {
                //var weapon = (Weapon)gamePlayer.HandsController.Item;
                var magazineFull = weapon.GetCurrentMagazine();
                //Logger.LogWarning("Magazine: " + magazineFull.LocalizedName());
                Item ammo = TakeChamberAmmo(weapon);

                if (ammo != null)
                {
                    //Logger.LogWarning("Ammo: " + ammo.LocalizedName());
                    bool isRoundChamber = false;
                    foreach (var item in magazineFull.Slots)
                    {
                        item.RemoveItem();
                        item.Add(ammo.CloneItem());
                        isRoundChamber = true;
                    } //Chamber Magazine
                    if (!isRoundChamber)
                    {
                        int difference = magazineFull.Cartridges.MaxCount - magazineFull.Cartridges.Count;
                        for (int i = 0; i < difference; i++)
                            magazineFull.Cartridges.Add(ammo.CloneItem(), false);
                    } //Normal magazine

                    for (int i = 0; i < weapon.Slots.Length; i++)
                    {
                        if (weapon.Slots[i] == weapon.GetMagazineSlot())
                        {
                            weapon.Slots[i].RemoveItem();
                            weapon.Slots[i].Add(magazineFull.CloneItem());
                            //Logger.LogWarning("Riempita arma");
                            break;
                        }
                    } //Replace the magazine with a full copy.
                }
            }
            catch (System.Exception e){Logger.LogWarning("Error reloading weapon: "+e);}
        }
    }
}
