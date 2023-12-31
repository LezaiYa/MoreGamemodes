using Il2CppSystem.Collections.Generic;
using AmongUs.GameOptions;
using static MoreGamemodes.Translator;

namespace MoreGamemodes
{
    public class CustomGamemode
    {
        public virtual void OnExile(GameData.PlayerInfo exiled)
        {

        }

        public virtual void OnSetFilterText(HauntMenuMinigame __instance)
        {
            if (__instance.HauntTarget.Data.Role.Role == RoleTypes.Scientist)
                __instance.FilterText.text = GetString("Scientist");
            if (__instance.HauntTarget.Data.Role.Role == RoleTypes.Engineer)
                __instance.FilterText.text = GetString("Engineer");
            if (__instance.HauntTarget.Data.Role.Role == RoleTypes.CrewmateGhost)
                __instance.FilterText.text = GetString("CrewmateGhost");
            if (__instance.HauntTarget.Data.Role.Role == RoleTypes.GuardianAngel)
                __instance.FilterText.text = GetString("GuardianAngel");
            if (__instance.HauntTarget.Data.Role.Role == RoleTypes.Shapeshifter)
                __instance.FilterText.text = GetString("Shapeshifter");
            if (__instance.HauntTarget.Data.Role.Role == RoleTypes.ImpostorGhost)
                __instance.FilterText.text = GetString("ImpostorGhost");
        }

        public virtual void OnHudUpate(HudManager __instance)
        {
            
        }

        public virtual void OnSetTaskText(TaskPanelBehaviour __instance, string str)
        {
            
        }

        public virtual void OnShowNormalMap(MapBehaviour __instance)
        {
            
        }

        public virtual void OnShowSabotageMap(MapBehaviour __instance)
        {
            
        }

        public virtual void OnToggleHighlight(PlayerControl __instance)
        {
            
        }

        public virtual List<PlayerControl> OnBeginCrewmatePrefix(IntroCutscene __instance)
        {
            var Team = new List<PlayerControl>();
            Team.Add(PlayerControl.LocalPlayer);
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (pc != PlayerControl.LocalPlayer)
                    Team.Add(pc);
            }
            return Team;
        }

        public virtual void OnBeginCrewmatePostfix(IntroCutscene __instance)
        {
            
        }

        public virtual void OnBeginImpostor(IntroCutscene __instance)
        {
            
        }

        public virtual void OnShowRole(IntroCutscene __instance)
        {
            
        }

        public virtual void OnVotingComplete()
        {
            
        }

        public virtual bool OnCastVote(MeetingHud __instance, byte srcPlayerId, byte suspectPlayerId)
        {
            return true;
        }

        public virtual void OnSelectRolesPrefix()
        {
            
        }

        public virtual void OnSelectRolesPostfix()
        {

        }

        public virtual void OnIntroDestroy()
        {
            
        }

        public virtual void OnPet(PlayerControl pc)
        {

        }

        public virtual bool OnCheckProtect(PlayerControl __instance, PlayerControl target)
        {
            return true;
        } 

        public virtual bool OnCheckMurder(PlayerControl killer, PlayerControl target)
        {
            return true;
        }

        public virtual void OnMurderPlayer(PlayerControl killer, PlayerControl target)
        {

        }

        public virtual bool OnCheckShapeshift(PlayerControl shapeshifter, PlayerControl target)
        {
            return true;
        }

        public virtual void OnShapeshift(PlayerControl shapeshifter, PlayerControl target)
        {

        }

        public virtual bool OnReportDeadBody(PlayerControl __instance, GameData.PlayerInfo target)
        {
            return true;
        }

        public virtual void OnFixedUpdate()
        {

        }

        public virtual void OnCompleteTask(PlayerControl __instance)
        {

        }

        public virtual bool OnCloseDoors(ShipStatus __instance)
        {
            return true;
        }

        public virtual bool OnUpdateSystem(ShipStatus __instance, SystemTypes systemType, PlayerControl player, byte amount)
        {
            return true;
        }

        public static CustomGamemode Instance;
        public Gamemodes Gamemode;
        public bool PetAction;
        public bool DisableTasks;
    }
}