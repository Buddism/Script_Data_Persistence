//intended for gamemodes
//$PersistenceDBPath = "config/server/PersistenceDB/";
if($PersistenceDBPath $= "")
{
	error("$PersistenceDBPath is undefined!");
	return;
}

exec("./Support_Data_Persistence.cs");

package PlayerPersistenceDBPackage
{
	function serverCmdChangeMap(%client, %mapName)
	{
		saveAllClientPersistenceDB();
		Parent::serverCmdChangeMap(%client, %mapName);
	}

	function GameConnection::savePersistenceDB(%client)
	{
		if(!%client.hasSpawnedOnce)
			return;

		//client connected, but did not auth, seems like hasSpawnedOnce should catch this but it apparently doesn't
		if(%client.getBLID() < 0)
			return;

		//open file
		%file = new FileObject();
		%filename = $PersistenceDBPath @ %client.getBLID() @ ".txt";
		%file.openForWrite(%filename);

		if(!%file)
		{
			error("ERROR: GameConnection::savePersistenceDB(" @ %client @ ") - failed to open file '" @ %filename @ "' for write");
			%file.delete();
			return;
		}

		echo("Saving PersistenceDB for BLID " @ %client.getBLID());


		//save all registered client tagged fields
		%file.writeLine(">CLIENT");
		%count = $PersistenceDB::FuncCount["CLIENT"];
		for(%i = 0; %i < %count; %i++)
		{
			%ret = call($PersistenceDB::Func["CLIENT", %i], %client);
			if(getFieldCount(%ret) >= 2)
				%file.writeLine(%ret);
		}
		PersistenceDB::SaveTaggedFields(%client, %file);


		//save player object
		%player = %client.player;
		if(isObject(%player))
		{
			%file.writeLine(">PLAYER");

			%count = $PersistenceDB::FuncCount["PLAYER"];
			for(%i = 0; %i < %count; %i++)
			{
				%ret = call($PersistenceDB::Func["PLAYER", %i], %player, %client);
				if(getFieldCount(%ret) >= 2)
					%file.writeLine(%ret);
			}

			PersistenceDB::SaveTaggedFields(%player, %file);
		}
		else
		{
			//dead?
			//save camera orbit point
			%camera = %client.camera;
			if(isObject(%camera))
			{
				%file.writeLine(">CAMERA");
				%count = $PersistenceDB::FuncCount["CAMERA"];
				for(%i = 0; %i < %count; %i++)
				{
					%ret = call($PersistenceDB::Func["CAMERA", %i], %camera, %client);
					if(getFieldCount(%ret) >= 2)
						%file.writeLine(%ret);
				}
				PersistenceDB::SaveTaggedFields(%camera, %file);
			}
		}

		//close file
		%file.close();
		%file.delete();
	}

	function PersistenceDB::SaveTaggedFields(%obj, %file)
	{
		%className = %obj.getClassName();
		%idx = 0;
		%taggedField = "";
		while(1)
		{
			%taggedField = %obj.getTaggedField(%idx);
			if(%taggedField $= "")
				break;

			%name  = getField (%taggedField, 0);
			%value = getFields(%taggedField, 1, 999);

			%saveLine = false;
			if($PersistenceDB::MatchName[%name] $= "1" && ($PersistenceDB::MatchClassName[%name] $= "" || $PersistenceDB::MatchClassName[%name] $= %className))
			{
				%saveLine = true;
			}
			else
			{
				for(%i = 0; %i < $PersistenceDB::MatchAll_Count; %i++)
				{
					%len = strlen($PersistenceDB::MatchAll_Entry[%i]);
					if( ($PersistenceDB::MatchAll_ClassName[%i] !$= "" && $PersistenceDB::MatchAll_ClassName[%i] !$= %className) || strnicmp($PersistenceDB::MatchAll_Entry[%i], %name, %len) != 0)
						continue;

					if($PersistenceDB::MatchAll_Datablock[%i] !$= "")
					{
						//before saving a datablock, we make sure it is the expected datablock type
						//if you hit this error you are probably doing something wrong
						if(!isObject(%value))
						{
							error("ERROR: PersistenceDB::SaveTaggedFields(" @ %obj @ ", " @ %file @ ") - (MatchAll \""@ $PersistenceDB::MatchAll_Entry[%i] @"\") tagged field " @ %name @ " => " @ %value @ " is not an object");
							break;
						}
						else if(%value.getClassName() $= $PersistenceDB::MatchAll_Datablock[%i])
						{
							%dbName = %value.getName();
							%file.writeLine(%name TAB %dbName);
							break;
						}
						else
						{
							error("ERROR: PersistenceDB::SaveTaggedFields(" @ %obj @ ", " @ %file @ ") - (MatchAll \""@ $PersistenceDB::MatchAll_Entry[%i] @"\") tagged field " @ %name @ " => " @ %value @ " is not a " @ $PersistenceDB::MatchDatablock[%name]);
							break;
						}
					}

					%saveLine = true;
					break;
				}
			}

			if(%saveLine)
			{
				//echo(%idx @ " saving " @ %name @ " => " @ %value);

				if($PersistenceDB::MatchDatablock[%name] !$= "" && %value !$= "0")
				{
					//before saving a datablock, we make sure it is the expected datablock type
					//if you hit this error you are probably doing something wrong
					if(!isObject(%value))
					{
						error("ERROR: PersistenceDB::SaveTaggedFields(" @ %obj @ ", " @ %file @ ") - tagged field " @ %name @ " => " @ %value @ " is not an object");
					}
					else if(%value.getClassName() $= $PersistenceDB::MatchDatablock[%name])
					{
						%dbName = %value.getName();
						%file.writeLine(%name TAB %dbName);
					}
					else
					{
						error("ERROR: PersistenceDB::SaveTaggedFields(" @ %obj @ ", " @ %file @ ") - tagged field " @ %name @ " => " @ %value @ " is not a " @ $PersistenceDB::MatchDatablock[%name]);
					}
				}
				else
				{
					%file.writeLine(%taggedField);
				}
			}

			%idx++;
		}
	}

	function GameConnection::loadPersistenceDB(%client)
	{
		//open file
		%file = new FileObject();
		%filename = $PersistenceDBPath @ %client.getBLID() @ ".txt";
		%file.openForRead(%filename);

		if(!%file)
		{
			error("ERROR: GameConnection::loadPersistenceDB(" @ %client @ " (BLID: " @ %client.getBLID() @ ")) - failed to open file '" @ %filename @ "' for write");
			%file.delete();
			return;
		}

		echo("Loading PersistenceDB for BLID " @ %client.getBLID());

		//read and assign data
		%currObj = 0;
		%gotPlayer = false;
		%gotCamera = false;
		while(!%file.isEOF())
		{
			%line = %file.readLine();

			if(%line $= ">CLIENT")
			{
				%currObj = %client;
				%currObjClass = "CLIENT";
				continue;
			}
			else if(%line $= ">PLAYER")
			{
				%gotPlayer = true;
				%currObj = %client.player;
				%currObjClass = "PLAYER";
				continue;
			}
			else if(%line $= ">CAMERA")
			{
				%gotCamera = true;
				%currObj = %client.camera;
				%currObjClass = "CAMERA";
				continue;
			}

			if(!isObject(%currObj))
				continue;

			%name  = getField (%line, 0);
			%value = getFields(%line, 1, 999);

			//special handling of some values
			if($PersistenceDB::SpecialMatch[%name] && ($PersistenceDB::SpecialMatchClass[%name] $= "" || $PersistenceDB::SpecialMatchClass[%name] $= %currObjClass))
			{
				%func = $PersistenceDB::SpecialMatchHandler[%name];
				switch$(%currObjClass)
				{
					case "PLAYER":
						call(%func, %line, %client.player, %client);
					case "CLIENT":
						call(%func, %line, %client);
					case "CAMERA":
						call(%func, %line, %client.camera, %client);
				}
			}
			else
			{
				%count = $PersistenceDB::SpecialMatchAll_Count;

				%matchedEntry = false;
				//see if it is already registered;
				for(%i = 0; %i < %count; %i++)
				{
					//$PersistenceDB::SpecialMatchAll_Entry		[$PersistenceDB::SpecialMatchAll_Count] = %name;
					//$PersistenceDB::SpecialMatchAll_ClassName [$PersistenceDB::SpecialMatchAll_Count] = %matchClassName;
					//$PersistenceDB::SpecialMatchAll_Handler		[$PersistenceDB::SpecialMatchAll_Count] = %handlerFunc;
					%len = strlen($PersistenceDB::MatchAll_Entry[%i]);
					if( ($PersistenceDB::SpecialMatchAll_ClassName[%i] !$= "" && $PersistenceDB::SpecialMatchAll_ClassName[%i] !$= %currObjClass) || strnicmp($PersistenceDB::SpecialMatchAll_Entry[%i], %name, %len) != 0)
						continue;
					%func = $PersistenceDB::SpecialMatchAll_Handler[%i];
					switch$(%currObjClass)
					{
						case "PLAYER":
							call(%func, %line, %client.player, %client);
						case "CLIENT":
							call(%func, %line, %client);
						case "CAMERA":
							call(%func, %line, %client.camera, %client);
					}
					%matchedEntry = true;
					break;
				}

				if(%matchedEntry)
					continue;

				//convert back from datablock name?
				if($PersistenceDB::MatchDataBlock[%name] !$= "")
				{
					if(%value !$= "0")
					{
						if(!isObject(%value))
						{
							//attempted to load a datablock reference that doesn't exist
							//save was probably made with an addon enabled that is currently disabled
							warn("WARNING: GameConnection::loadPersistenceDB(" @ %client @ " (BLID: " @ %client.getBLID() @ ")) - loading " @ %name @ " => " @ %value @ " as datablock, '" @ %value @ "' is not an object");
							continue;
						}

						%value = %value.getId();
						if(%value.getClassName() !$= $PersistenceDB::MatchDataBlock[%name])
						{
							warn("WARNING: GameConnection::loadPersistenceDB(" @ %client @ " (BLID: " @ %client.getBLID() @ ")) - loading " @ %name @ " => " @ %value @ " as datablock, '" @ %value @ "' is not a " @ $PersistenceDB::MatchDataBlock[%name]);
							continue;
						}
					}
				}

				%cmd = "%currObj." @ %name @ " = \"" @ %value @ "\";";
				eval(%cmd);
			}
		}

		//close file
		%file.close();
		%file.delete();

		%client.applyPersistenceDB(%gotPlayer, %gotCamera);
		if(isObject(%client.player))
			commandToClient(%client, 'ShowEnergyBar', %client.player.getDataBlock().showEnergyBar);

		%client.schedulePersistenceDBSave(1);
	}

	function GameConnection::applyPersistenceDB(%client, %gotPlayer, %gotCamera)
	{
		//this function is called after PersistenceDB has been loaded
		//if you want to restore client owned objects then this is the function you package into
		%camera = %client.camera;
		%player = %client.player;

		echo("Applying PersistenceDB" SPC %gotPlayer SPC %gotCamera);

		%count = $PersistenceDB::ApplyFuncCount + 0;
		for(%i = 0; %i < %count; %i++)
			call($PersistenceDB::ApplyFunc[%i], %client, %player, %camera);
	}

   function GameConnection::onClientEnterGame(%client)
   {
      if(%client.inventory0 !$= "" && %client.inventory0 !$= "0") //no clue what this is for
         commandToClient(%client, 'CancelAutoBrickBuy');
      Parent::onClientEnterGame(%client);
      %client.loadPersistenceDB();
   }

   function GameConnection::onClientLeaveGame(%client)
   {
      %client.savePersistenceDB();
      Parent::onClientLeaveGame(%client);
   }

   function doQuitGame()
   {
      //if we're hosting and hit "quit", save all clients first
      if(isObject(ServerGroup))
      {
         saveAllClientPersistenceDB();
      }

      Parent::doQuitGame();
   }

   function saveAllClientPersistenceDB()
   {
      %count = clientGroup.getCount();
      for(%i = 0; %i < %count; %i++)
      {
         %cl = clientGroup.getObject(%i);
         %cl.savePersistenceDB();
      }
   }

   function loadAllClientPersistenceDBDB()
   {
      %count = clientGroup.getCount();
      for(%i = 0; %i < %count; %i++)
      {
         %cl = clientGroup.getObject(%i);
         if(!%cl.hasSpawnedOnce)
            %cl.loadPersistenceDBDB();
      }
   }

   function GameConnection::schedulePersistenceDBSave(%client, %firstTime)
   {
      if(isEventPending(%client.PersistenceDBDchedule))
         cancel(%client.PersistenceDBDchedule);

      if(!%firstTime)                                     //don't save immediately after loading
         if(!isEventPending($LoadSaveFile_Tick_Schedule)) //don't save during brick loading
            %client.savePersistenceDB();

      //save every 5 minutes
      %client.PersistenceDBSchedule = %client.schedule(5 * 60 * 1000, schedulePersistenceDBSave);
   }

};
activatePackage(PlayerPersistenceDBPackage);
