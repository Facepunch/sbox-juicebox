# S&box Juicebox API

This is the [S&box](https://sbox.facepunch.com/) implementation of the Juicebox API to create party game sessions on [juicebox.facepunch.com](https://juicebox.facepunch.com).

For a sample game using this library, see [Juicebox](https://github.com/Facepunch/sbox-juicebox).

Feel free to report issues with the [juicebox.facepunch.com](https://juicebox.facepunch.com) website in this repo.

## Getting started

This repository is published as a library. You can add [`facepunch.juicebox_api`](https://asset.party/facepunch/juicebox_api) as a package reference in your S&box project to reference the latest version of the API.

With the package referenced you just need to get the right namespace:
```cs
using Juicebox;
```

Everything revolves around sessions. You'll need to create a session for players to join and interact with your game:
```cs
var session = new JuiceboxSession();
await session.Start();
```

After the session is created and started, the `JoinPassword` should be shown to players in some way so that they can join the game from [juicebox.facepunch.com](https://juicebox.facepunch.com)
```cs
Log.Info($"Join now! Password is {session.JoinPassword}");
```

You can control what is shown to players on the web client using the `Display` method:
```cs
await session.Display( new JuiceboxDisplay
{
    Stage = new JuiceboxStage
    {
        Title = "Hello World"
    }
} );
```

This would display "Hello World" for all players.

TODO: Make a dev web page which allows designing and previewing the display models

The session can be closed by disposing it. This is important because it will inform all players on the web client that the session is no longer active - and it cleans up resources in S&box:
```cs
session.Dispose();
```

### Forms

You can prompt players for input by displaying a form for them. Forms can have multiple inputs if needed:
```cs
await session.Display( new JuiceboxDisplay
{
    Stage = new JuiceboxStage
    {
        Title = "Tell me about yourself"
    },
    Form = new JuiceboxForm
    {
        Header = "About you",
        Controls = new List<JuiceboxFormControl>
        {
            new JuiceboxInput { Key = "color", Label = "Favorite color", Placeholder = "Green", MaxLength = 20 },
            new JuiceboxInput { Key = "age", Label = "Age", Placeholder = "12", MaxLength = 3 },
        }
    }
} );
```

Calling `Display` will overwrite the previously set display entirely. The updated display shown above would include a form with two inputs and a submit button.

When players type in their responses and click the submit button they will be sent back to your session and the `OnResponseReceived` event would be called:
```cs
session.OnResponseReceived += ( session, player, data ) =>
{
    Log.Info( $"{player.Name} is {data["age"]} years old and their favorite color is {data["color"]}" );
};
```

The web client only allows forms to be submitted once but your game should double check! People may try to cheat by sending multiple submissions.

### Action buttons

Sometimes you may need a button which can be pressed any number of times to do something in your game. Buttons can be added into the form area which behave a little bit differently:

1. All buttons are shown below the form inputs and submit button, if there is one
2. Buttons do not submit the form, if there is one
3. Buttons can be pressed more than once, regardless of the form's state, if there is one

```cs
await session.Display( new JuiceboxDisplay
{
    Stage = new JuiceboxStage
    {
        Title = "Are you ready?"
    },
    Form = new JuiceboxForm
    {
        Controls = new List<JuiceboxFormControl>
        {
            new JuiceboxButton { Key = "start", Label = "Start Game" },
        }
    }
} );
```

Because button presses do not submit the form, they do not trigger the `OnResponseReceived` event when pressed. Pressing buttons will trigger the `OnActionReceived` event instead:
```cs
session.OnActionReceived += ( session, player, key ) =>
{
    if ( key == "start" )
    {
        Log.Info( $"{player.Name} is ready to start the game!" );
    }
};
```
