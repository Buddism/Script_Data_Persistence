

RegisterPersistenceDBVar("tool0", false, "ItemData"); //save the variable named "tool0" matching DB "itemData" on all persistence objects
RegisterPersistenceDBVar("tool0", false, "ItemData", "Player"); //only save "tool0" on the player
RegisterPersistenceVar("foundTreasureChest_", true, ""); //save variables prefixed with foundTreasureChest_


//function must return VARNAME TAB DATA
function PSDB_Example(%player)
{
	return "transform" TAB %player.getTransform();
}

RegisterPersistenceDBSaveFunc("PSDB_Example", "PLAYER");
//function is called if the class matches when saving persistence variables
//RegisterPersistenceDBSaveFunc( %name,       %matchClass [Player | GameConnection | Camera])

//DB Func CLASS TYPE ARGS
// "PLAYER" function myPLAYERFunc(%player, %client) {}
// "CLIENT" function myCLIENTFunc(%client) {}
// "CAMERA" function myCAMERAFunc(%camera, %client) {}

function tool0handlerFunc(%taggedField, %player, %client)
{
	%player.tool[0] = getField(%taggedField, 1);
}
RegisterPersistenceDBVarHandler("tool0", false, "Player", "tool0handlerFunc")
function toolALLhandlerFunc(%taggedField, %player, %client)
{
	%toolID = getField(%taggedField, 0);
	%toolNum = getSubStr(%toolID, 4, 100);
	%player.tool[%toolNum] = getField(%taggedField, 1);
}
RegisterPersistenceDBVarHandler("tool", true, "Player", "toolALLhandlerFunc")

//handler func is ran when persistence is applying to the PLAYER/CLIENT/CAMERA
//function RegisterPersistenceDBVarHandler(%name, %matchAll, %matchClassName [PLAYER | CLIENT | CAMERA], %handlerFunc)



function Persistence_Player_saveTransform(%player)
{
    return "transform" TAB %player.getTransform();
}
//must return 2 fields to save a line
RegisterPersistenceDBSaveFunc("Persistence_Player_saveTransform", "PLAYER");

function Persistence_Player_setTransform(%taggedField, %player, %client)
{
    %transform = getField(%taggedField, 1);
    %player.setTransform(%transform);
}
RegisterPersistenceDBVarHandler("transform", false, "Player", "Persistence_Player_setTransform")
//DB Var Handler Func CLASS TYPE ARGS
// "PLAYER" function myPLAYERFunc(%player, %client) {}
// "CLIENT" function myCLIENTFunc(%client) {}
// "CAMERA" function myCAMERAFunc(%camera, %client) {}
