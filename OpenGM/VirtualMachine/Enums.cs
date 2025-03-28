namespace OpenGM.VirtualMachine;

/// <summary>
/// result of the execution of an instruction
/// </summary>
public enum ExecutionResult
{
	Success,
	Failed,
	ReturnedValue,
	JumpedToLabel,
	JumpedToEnd
}

public enum VariableType
{
	None,
	Global,
	Self,
	Local,
	Index,
	Other,
	BuiltIn,
	Argument,
	Stacktop
}

public enum VariablePrefix
{
	None,
	Stacktop,
	Array,
	ArrayPopAF,
	ArrayPushAF
}

public enum EventType
{
	None,
	Create,
	Destroy,
	Alarm,
	Step,
	Collision,
	Keyboard,
	Mouse,
	Other,
	Draw,
	KeyPress,
	KeyRelease,
	Trigger,
	CleanUp,
	Gesture, // not in UNDERTALE
	PreCreate // not in UNDERTALE
}

public enum VMOpcode
{
	CONV,
	MUL,
	DIV,
	REM,
	MOD,
	ADD,
	SUB,
	AND,
	OR,
	XOR,
	NEG,
	NOT,
	SHL,
	SHR,
	CMP,
	POP,
	DUP,
	RET,
	EXIT,
	POPZ,
	B,
	BT,
	BF,
	PUSHENV,
	POPENV,
	PUSH,
	PUSHLOC,
	PUSHGLB,
	PUSHBLTN,
	PUSHI,
	CALL,
	CALLV,
	BREAK,
	CHKINDEX,
	SETOWNER,
	PUSHAF,
	POPAF,
	SAVEAREF,
	RESTOREAREF
}

public enum VMType
{
	None = -1,
	d = 0,
	// float = 1
	i = 2,
	l = 3,
	b = 4,
	v = 5,
	s = 6,
	// GMDebug_StringPatch = 7
	// Delete = 8
	// Undefined = 9
	// PtrType = 10
	e = 15
}

public enum VMComparison
{
	None,
	LT,
	LTE,
	EQ,
	NEQ,
	GTE,
	GT
}