# ModAPI 2

Hi! 

Welcome to the GitHub repository of ModAPI 2.
ModAPI 2 aims to bring modding to Unity (and also managed) games with some new paradigms like the ability to replace fields with properties and chaining methods with callstack-aware chain progression. You can find more information about how these systems work below.

# Paradigms

## Ease of use by overriding

All modding happens by creating a project and inheriting from original game types and overriding methods. In order for you to do so, ModAPI 2 will create a mod library which will open up all types and make all methods overrideable.

A simple mod could look like this
```
public class MyMod : GameClass
{
	public override void SomeMethod()
	{
		// do something
		// call to next method in chain or original method
		base.SomeMethod(); 
	}
}
```

## Field overrides

In order to be as versatile as possible ModAPI 2 offers you to override fields. The mod library will automatically replace all fields with properties. If you override one of these properties ModAPI 2 will change the field to a property in the game assemblies too.

A simple field override looks like this:
```
public class MyMod : GameClass
{
	public override StringFunction responseText
	{
		get
		{
			return () => 
			{
				return "some prefix" + base.responseText();
			};
		}
	}
}
```

## Callstack-Aware chaining

To make chaining as easy as possible ModAPI transpiles your code for you. Let's have a look at this example:

```
public class MyMod : GameClass
{
	public override StringFunction responseText
	{
		get
		{
			return () => 
			{
				return "some prefix" + base.responseText();
			};
		}
	}
}
```
This example uses a lambda-expression which will get heavily transpiled.

ModAPI will create code which looks like this:
```
public class MyMod : GameClass
{
	[CompilerGenerated]
	private class <>c__DisplayClass2_0 : System.Object 
	{
		public GameClass self;
		public GameClass.__ModAPI_get_responseText_Delegate[] chain;
		public int num;
		
		public void b__0()
		{
			return "some prefix" + chain[num](self, chain, num + 1)();
		}
	}
	
	[ModAPI.Injection(ModAPI.InjectionType.Chain, ...)]
	public static StringFunction get_responseText(
		GameClass self, 
		GameClass.__ModAPI_get_responseText_Delegate[] chain,
		int num)
	{
		var displayClass = new <>c__DisplayClass2_0()
		{
			self = self,
			chain = chain,
			num = num
		};
		displayClass.b__0();
	}
}
```

The original game method will be changed as follows:
```
public class GameClass
{
	public delegate StringFunction __ModAPI_get_responseText_Delegate(
		GameClass self,
		__ModAPI_get_responseText_Delegate[] chain,
		int num);
	public StringFunction responseText;
	private static __ModAPI_get_responseText_Delegate[] __ModAPI_get_responseText_Chain;
	public StringFunction __ModAPI_responseText 
	{
		get 
		{
			if (__ModAPI_get_responseText_Chain == null)
			{
				__ModAPI_get_responseText_Chain = new[2];
				__ModAPI_get_responseText_Chain[0] = MyMod.get_responseText;
				__ModAPI_get_responseText_Chain[1] = get_responseText_Original;
			}
			return __ModAPI_get_responseText_Chain[0](this, __ModAPI_get_responseText_Chain, 1);
		}
		set 
		{
			responseText = value;
		}
	}

	public StringFunction get_responseText_Original()
	{
		return responseText;
	}
}
```

As you can see ModAPI does a lot of work for you, so you can concentrate on what really matters: Modding.

## What about hooks?

Some people prefer to use hooks and so ModAPI offers you to hook into methods. Let's take the following game method:
```
public int Test(int a);
```
Let's create two hooks:
```
[ModAPI.Injection(InjectionType.HookBefore)]
// note that the return type is void
public override void Test(int a)
{
	// do something
}

[ModAPI.Injection(InjectionType.HookAfter)]
// note that the return type is void and second parameter is a reference
// for the return value
public override void Test(int a, ref int retValue)
{
	// do something
}
```

# License

ModAPI 2 is licensed under GPLv3 with the limitations added by Commons Clause. These limitations are in place because we had problems in the past with people selling the software as-is and because this isn't what I think modding should be: Free!

You can find the license in the file LICENSE in this repository.