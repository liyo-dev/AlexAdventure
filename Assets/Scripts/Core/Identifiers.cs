public enum AbilityId { PhysicalAttack, MagicAttack, Dash, Block }
public enum SpellId   { None, Fireball, IceSpike, Storm, Lightning, FireSpecial }
public enum DamageKind { Physical, Magic, Special }
public enum QuesStateEnum { NotStarted, Active, Completed }
public enum MagicSlot   { Left, Right, Special }
public enum MagicKind   { Projectile, Special }
public enum MagicElement{ Fire, Ice, Storm, Light }
public enum SpellSlotType { Any, SpecialOnly }
public enum QuestState { Inactive = 0, Active = 1, Completed = 2, Failed = 3 }

// Enumerados para localización
public enum UITextId 
{
    // Menu principal
    MainMenu_NewGame,
    MainMenu_Continue,
    MainMenu_Settings,
    MainMenu_Exit,
    
    // Settings
    Settings_Language,
    Settings_Audio,
    Settings_Graphics,
    Settings_Controls,
    Settings_Back,
    
    // Gameplay UI
    UI_Health,
    UI_Mana,
    UI_Level,
    UI_Experience,
    
    // Diálogos comunes
    Dialogue_Continue,
    Dialogue_Skip,
    Dialogue_End,
    
    // Interacciones
    Interact_Press,
    Interact_Talk,
    Interact_Examine,
    Interact_PickUp,
    Interact_Open,
    
    // Sistema
    System_Loading,
    System_Saving,
    System_GameSaved,
    System_Error
}

public enum DialogueId
{
    // NPCs
    NPC_Villager_01,
    NPC_Merchant_01,
    NPC_Guard_01,
    
    // Objetos
    Object_Sign_01,
    Object_Book_01,
    Object_Chest_01,
    
    // Tutoriales
    Tutorial_Movement,
    Tutorial_Combat,
    Tutorial_Magic
}
