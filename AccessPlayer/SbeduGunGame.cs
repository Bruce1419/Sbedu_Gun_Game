using EFT;
using EFT.InventoryLogic;
using Comfort.Common;
using BepInEx;
using System.Collections.Generic;

namespace Sbedu_Gun_Game
{
    [BepInPlugin("com.Sbedu_Gun_Game", "Sbedu_Gun_Game", "1.0.0")]
    public class SbeduGunGame : BaseUnityPlugin
    {
        private static GameWorld gameWorld;
        public static Player gamePlayer = null;
        public static List<int> listAiID = new List<int>();
        private void Awake()
        {
            Logger.LogInfo("Sbedu Gun Game is loading!...");
            Logger.LogInfo("Sbedu Gun Game has loaded!");
        }

        private void Update()
        {
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
                    if (!listAiID.Contains(item.Id) && item.Id != gamePlayer.Id)
                    {
                        Logger.LogWarning("Adding AI ID: " + item.Id);
                        item.OnPlayerDead += (Player killedGuy, Player killer, DamageInfo damageInfo, EBodyPart part) =>
                        {
                            Logger.LogWarning("Killed AI: " + killedGuy.Id);
                            if (killer.Id == gamePlayer.Id && killedGuy.HandsController.Item is Weapon)
                            {
                                Logger.LogWarning("Taking weapon of: " + item.Id);
                                var weapon = (Weapon)item.HandsController.Item;
                                //var magazine = weapon.GetCurrentMagazine();
                                gamePlayer.SetInHands(weapon, null);
                            }
                            Logger.LogWarning("Removing: " + killedGuy.Id);
                            listAiID.Remove(item.Id);
                        };
                        listAiID.Add(item.Id);
                    }
                }
            }
            else
                listAiID.Clear();
        }
    }
}
