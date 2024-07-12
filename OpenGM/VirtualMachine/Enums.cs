namespace DELTARUNITYStandalone.VirtualMachine;

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
	Argument
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
	Gesture,
	PreCreate
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
	None,
	i,
	v,
	b,
	d,
	e,
	s,
	l
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