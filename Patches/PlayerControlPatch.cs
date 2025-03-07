﻿using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;
using AmongUs.GameOptions;
using Hazel;
using InnerNet;

namespace MoreGamemodes
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckProtect))]
    class CheckProtectPatch 
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            return CustomGamemode.Instance.OnCheckProtect(__instance, target);
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckMurder))]
    class CheckMurderPatch 
    {
        public static Dictionary<byte, float> TimeSinceLastKill;
        public static void Update()
        {
            if (!AmongUsClient.Instance.AmHost) return;
            for (byte i = 0; i < 15; i++)
            {
                if (TimeSinceLastKill.ContainsKey(i))
                {
                    TimeSinceLastKill[i] += Time.deltaTime;
                    if (15f < TimeSinceLastKill[i]) TimeSinceLastKill.Remove(i);
                }
            }
        }
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target) 
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            PlayerControl killer = __instance;
            if (!CheckForInvalidMurdering(killer, target))
                return false;
            
            if (CustomGamemode.Instance.OnCheckMurder(killer, target))
            {
                killer.SyncPlayerSettings();
                killer.RpcMurderPlayer(target, true);
            }     

            return false;
        }
        public static bool CheckForInvalidMurdering(PlayerControl killer, PlayerControl target)
        {
            if (AmongUsClient.Instance.IsGameOver || !AmongUsClient.Instance.AmHost) return false;
		    if (!target || killer.Data.IsDead || killer.Data.Disconnected) return false;
		    GameData.PlayerInfo data = target.Data;
		    if (data == null || data.IsDead || target.inVent || target.MyPhysics.Animations.IsPlayingEnterVentAnimation() || target.MyPhysics.Animations.IsPlayingAnyLadderAnimation() || target.inMovingPlat) return false;
		    if (MeetingHud.Instance) return false;
            float minTime = Mathf.Max(0.02f, AmongUsClient.Instance.Ping / 1000f * 6f);
            if (TimeSinceLastKill.TryGetValue(killer.PlayerId, out var time) && time < minTime) return false;
            TimeSinceLastKill[killer.PlayerId] = 0f;
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.MurderPlayer))]
    class MurderPlayerPatch
    {
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] MurderResultFlags resultFlags)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            PlayerControl killer = __instance;
            if (!target.Data.IsDead) return;

            if (target.GetDeathReason() == DeathReasons.Alive)
                target.RpcSetDeathReason(DeathReasons.Killed);
            CustomGamemode.Instance.OnMurderPlayer(killer, target);
            if (target.Data.Role.IsImpostor && Options.CurrentGamemode != Gamemodes.BombTag && Options.CurrentGamemode != Gamemodes.BattleRoyale && Options.CurrentGamemode != Gamemodes.KillOrDie)
                target.RpcSetRole(RoleTypes.ImpostorGhost);
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckShapeshift))]
    class CheckShapeshiftPatch 
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] bool shouldAnimate) 
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            PlayerControl shapeshifter = __instance;
            if (!CheckForInvalidShapeshifting(shapeshifter, target, shouldAnimate))
            {
                shapeshifter.RpcRejectShapeshift();
                return false;
            } 
            
            if (!shouldAnimate)
                shapeshifter.RpcShapeshift(target, shouldAnimate);
            else if (CustomGamemode.Instance.OnCheckShapeshift(shapeshifter, target))
                shapeshifter.RpcShapeshift(target, shouldAnimate);
            else
                shapeshifter.RpcRejectShapeshift();
            
            return false;
        }
        public static bool CheckForInvalidShapeshifting(PlayerControl shapeshifter, PlayerControl target, bool shouldAnimate)
        {
            if (AmongUsClient.Instance.IsGameOver || !AmongUsClient.Instance.AmHost) return false;
		    if (!target || target.Data == null || shapeshifter.Data.IsDead || shapeshifter.Data.Disconnected) return false;
		    if (target.IsMushroomMixupActive() && shouldAnimate) return false;
            if (MeetingHud.Instance && shouldAnimate) return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Shapeshift))]
    class ShapeshiftPatch
    {
        public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] bool animate)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            var shapeshifter = __instance;
            var shapeshifting = shapeshifter != target;
            switch (shapeshifting)
            {
                case true:
                    Main.AllShapeshifts[shapeshifter.PlayerId] = target.PlayerId;
                    break;
                case false:
                    Main.AllShapeshifts[shapeshifter.PlayerId] = shapeshifter.PlayerId;
                    break;
            }
            new LateTask(() =>
            {
                if (MeetingHud.Instance) return;
                foreach (var pc in PlayerControl.AllPlayerControls)
                    shapeshifter.RpcSetNamePrivate(shapeshifter.BuildPlayerName(pc, false), pc, true);
            }, 1.32f, "Set Shapeshift Appearance");
            CustomGamemode.Instance.OnShapeshift(shapeshifter, target);
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.ReportDeadBody))]
    class ReportDeadBodyPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] GameData.PlayerInfo target)
        {
            if (!CustomGamemode.Instance.OnReportDeadBody(__instance, target)) return false;
            Main.IsMeeting = true;
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                foreach (var ar in PlayerControl.AllPlayerControls)
                    pc.RpcSetNamePrivate(pc.BuildPlayerName(ar, true), ar, true);  
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixedUpdate))]
    static class FixedUpdatePatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            if (Main.GameStarted && __instance == PlayerControl.LocalPlayer)
            {
                Main.Timer += Time.fixedDeltaTime;
            }
            if (!AmongUsClient.Instance.AmHost) return;
            if (!__instance.AmOwner) return;
            if (Main.GameStarted && !Main.IsMeeting)
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    foreach (var ar in PlayerControl.AllPlayerControls)
                        pc.RpcSetNamePrivate(pc.BuildPlayerName(ar, false), ar, false);
                }
            }
            if (Main.GameStarted)
                CustomGamemode.Instance.OnFixedUpdate();
            if (Options.MidGameChat.GetBool() && Options.ProximityChat.GetBool())
            {
                foreach (var pc in PlayerControl.AllPlayerControls)
                {
                    for (int i = 0; i < Main.ProximityMessages[pc.PlayerId].Count; ++i)
                    {
                        Main.ProximityMessages[pc.PlayerId][i] = (Main.ProximityMessages[pc.PlayerId][i].Item1, Main.ProximityMessages[pc.PlayerId][i].Item2 + Time.fixedDeltaTime);
                        if (Main.ProximityMessages[pc.PlayerId][i].Item2 > 3f + (Main.ProximityMessages[pc.PlayerId][i].Item1.Length / 10f))
                        {
                            Main.ProximityMessages[pc.PlayerId].RemoveAt(i);
                            --i;
                        }
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(PlayerPhysics), nameof(PlayerPhysics.CoEnterVent))]
    class CoEnterVentPatch
    {
        public static bool Prefix(PlayerPhysics __instance, [HarmonyArgument(0)] int id)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            if (!__instance.myPlayer.CanVent())
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, -1);
                writer.WritePacked(127);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
                new LateTask(() =>
                {
                    MessageWriter writer2 = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, (byte)RpcCalls.BootFromVent, SendOption.Reliable, -1);
                    writer2.Write(id);
                    AmongUsClient.Instance.FinishRpcImmediately(writer2);
                }, 0.5f, "Fix DesyncImpostor Stuck");
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CompleteTask))]
    class CompleteTaskPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            var pc = __instance;

            CustomGamemode.Instance.OnCompleteTask(pc);
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcUsePlatform))]
    class RpcUsePlatformPatch
    {
        public static bool Prefix(PlayerControl __instance)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            if (Options.DisableGapPlatform.GetBool()) return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.CheckUseZipline))]
    class CheckUseZiplinePatch
    {
        public static bool Prefix(PlayerControl __instance)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            if (Options.DisableZipline.GetBool())
                return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.SetRole))]
    class SetRolePatch
    {
        public static void Postfix(PlayerControl __instance, [HarmonyArgument(0)] RoleTypes roleType)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (!RoleManager.IsGhostRole(roleType))
                Main.StandardRoles[__instance.PlayerId] = roleType;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole))]
    class RpcSetRolePatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] RoleTypes roleType)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            if (Main.StandardRoles.ContainsKey(__instance.PlayerId) && !RoleManager.IsGhostRole(roleType))
                return false;
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcMurderPlayer))]
    class RpcMurderPlayerPatch
    {
        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] PlayerControl target, [HarmonyArgument(1)] bool didSucceed)
        {
            if (!AmongUsClient.Instance.AmHost) return true;
            MurderResultFlags murderResultFlags = didSucceed ? MurderResultFlags.Succeeded : MurderResultFlags.FailedError;
            if (murderResultFlags == MurderResultFlags.Succeeded && target.protectedByGuardianId > -1)
                murderResultFlags= MurderResultFlags.FailedProtected;
            if (murderResultFlags != MurderResultFlags.FailedError)
                __instance.SyncPlayerSettings();
            if (AmongUsClient.Instance.AmClient)
		    {
		    	__instance.MurderPlayer(target, murderResultFlags);
		    }
		    MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(__instance.NetId, 12, SendOption.Reliable, -1);
		    messageWriter.WriteNetObject(target);
		    messageWriter.Write((int)murderResultFlags);
		    AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Die))]
    class PlayerDiePatch
    {
        public static void Prefix(PlayerControl __instance, [HarmonyArgument(0)] DeathReason reason, [HarmonyArgument(1)] bool assignGhostRole)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (Options.CurrentGamemode != Gamemodes.PaintBattle)
                __instance.RpcSetPet("");
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.FixMixedUpOutfit))]
    class FixMixedUpOutfitPatch
    {
        public static void Postfix(PlayerControl __instance)
        {
            if (!AmongUsClient.Instance.AmHost) return;
            if (MeetingHud.Instance) return;
            new LateTask(() =>
            {
                if (!MeetingHud.Instance)
                {
                    foreach (var pc in PlayerControl.AllPlayerControls)
                        __instance.RpcSetNamePrivate(__instance.BuildPlayerName(pc, false), pc, true);
                }
            }, 1f, "Fix After MixUp Name");
        }
    }
}