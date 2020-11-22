//check example.cs for details

function isPersistenceDBSaveFunc(%name, %matchClass)
{
	%count = $PersistenceDB::FuncCount[%matchClass];
	for(%i = 0; %i < %count; %i++)
		if($PersistenceDB::Func[%matchClass, %i] $= %name)
			return 1;

	return 0;
}

function RegisterPersistenceDBSaveFunc(%name, %matchClass)
{
	if(%matchClass $= "" || %matchClass $= "all")
	{
		%class[0] = "CLIENT";
		%class[1] = "PLAYER";
		%class[2] = "CAMERA";
		for(%i = 0; %i < 3; %i++)
		{
			%matchClass = %class[%i];
			if(isPersistenceDBSaveFunc(%name, %matchClass)) //is it already registered as a persistence function
				break;

			%count = $PersistenceDB::FuncCount[%matchClass] + 0; //+ 0 converts to integer

			$PersistenceDB::Func[%matchClass, %count] = %name;
			$PersistenceDB::FuncCount[%class]++;
		}
		return 1;
	} else if(%matchClass $= "CLIENT" || %matchClass $= "PLAYER" || %matchClass $= "CAMERA")
	{
		if(isPersistenceDBSaveFunc(%name, %matchClass)) //is it already registered as a persistence function
			return;

		%count = $PersistenceDB::FuncCount[%matchClass] + 0; //+ 0 converts to integer

		$PersistenceDB::Func[%matchClass, %count] = %name;
		$PersistenceDB::FuncCount[%matchClass]++;

		return 1;
	}
	else
	{
		error("RegisterPersistenceDBSaveFunc: \""@ %matchClass @"\" must be of type CLIENT, PLAYER, or CAMERA");
		return 0;
	}
}




function RegisterPersistenceDBVarHandler(%name, %matchAll, %matchClassName, %handlerFunc)
{
	if(%handlerFunc $= "")
	{
		error("%handlerFunc cannot be undefined");
		error("usage: RegisterPersistenceDBVarHandler(%name, %matchAll, %matchClassName, %handlerFunc)");
		return 0;
	}
	if(%matchClass !$= "CLIENT" && %matchClass !$= "PLAYER" && %matchClass !$= "CAMERA" && %matchClass !$= "")
	{
		error("RegisterPersistenceDBVarHandler: \""@ %matchClass @"\" must be of type CLIENT, PLAYER, or CAMERA");
		return 0;
	}
	if(%matchAll)
	{
		//we need these in a list
		$PersistenceDB::SpecialMatchAll_Count = mFloor($PersistenceDB::SpecialMatchAll_Count);

		//see if it is already registered;
		for(%i = 0; %i < $PersistenceDB::SpecialMatchAll_Count; %i++)
		{
			if($PersistenceDB::SpecialMatchAll_Entry[%i] $= %name)
				return 0;
		}

		//not registered yet, add it
		$PersistenceDB::SpecialMatchAll_Entry[$PersistenceDB::SpecialMatchAll_Count] = %name;
		$PersistenceDB::SpecialMatchAll_ClassName[$PersistenceDB::SpecialMatchAll_Count] = %matchClassName;
		$PersistenceDB::SpecialMatchAll_Handler[$PersistenceDB::SpecialMatchAll_Count] = %handlerFunc;

		$PersistenceDB::SpecialMatchAll_Count++;

		return 1;
	}
	else
	{
		//simple name match
		$PersistenceDB::SpecialMatch[%name] = true;
		$PersistenceDB::SpecialMatchClass[%name] = %matchClassName;
		$PersistenceDB::SpecialMatchHandler[%name] = %handlerFunc;

		return 1;
	}
}

function RegisterPersistenceDBVar(%name, %matchAll, %matchDataBlock, %matchClassName)
{
	if(%matchAll)
	{
		//we need these in a list
		$PersistenceDB::MatchAll_Count = mFloor($PersistenceDB::MatchAll_Count);

		//see if it is already registered;
		for(%i = 0; %i < $PersistenceDB::MatchAll_Count; %i++)
		{
			if($PersistenceDB::MatchAll_Entry[%i] $= %name)
				return 0;
		}

		//not registered yet, add it
		$PersistenceDB::MatchAll_Entry[$PersistenceDB::MatchAll_Count] = %name;
		$PersistenceDB::MatchAll_ClassName[$PersistenceDB::MatchAll_Count] = %matchClassName;
		$PersistenceDB::MatchAll_Count++;

		return 1;
	}
	else
	{
		//simple name match
		$PersistenceDB::MatchName[%name] = true;
		$PersistenceDB::MatchDatablock[%name] = %matchDataBlock;
		$PersistenceDB::MatchClassName[%name] = %matchClassName;

		return 1;
	}
}
