# Indigo ECS

Indigo ECS is a simple entity-component-system library written in C#, intended for game development. It's still under development, but has reached a functional point where I've built a small game with it.

Key Features:
- **Determinism**: As long as you pass in the same input for each frame, you can rely on the game state always resolving to the same values. Note: Due to this feature, floating point values should not be used as component state.
- **State Rewind**: Easily rewind the game state backwards in time.
- **Entity Querying**: Efficent entity state querying.
- **No Garbage Collection**: All component state is pooled in a shared pool. This means no runtime allocations will occur after the start, and no de-allocations will occur, which means the GC will not trigger, hurting the game experience.
- **Aliases**: Group component types into 'alias' groups, allowing convenient accessing of component state. You can think of an alias as the equivalent of a type in traditional OOP.

 This library focuses on the organizational benefits of ecs, and does not attempt to optimize for cache friendliness like you might find in [flecs](https://github.com/SanderMertens/flecs) or other well known ecs libraries.

## Dependencies

- [**IndigoECS Code Generator**](https://github.com/rdshack/ecs_gen/tree/main): Technically not a dependency, but used to generate app-specific game state code consumed by the core ecs framework. 
- [**Google Flatbuffers**](https://github.com/google/flatbuffers): Used for game state serialization.
- [**FixedMath.NET**](https://github.com/asik/FixedMath.Net/tree/master?tab=License-1-ov-file): Fixed-point math library. No floats should be used in game-sim to guarantee determinism.

Flatbuffers and FixedMath.NET are included in the code generation library as compiled dlls, and are copied over to your game project as part of the code generation pipeline. Use later versions at your own risk.

## Definitions

### Entity
An entity is simply an identifier that binds a broup of components together. This is represented by the struct 'EntityId' which is just 64-bit unsigned integer with some utility methods.

### Component
A component represents a logical chunk of data. Components should be designed to be as small as possible. Essentially, all fields in a component ideally should depend upon the other fields in all cases. If there are situations where part of a component's state is not relevant, that's a sign that a new comopnent should be created.

### System
Systems represent the logic of the game. Systems are stateless, and simply perform operations on entities that contain the components they care about. Systems are executed sequentially each game tick, always in the same order.

### Archetypes / Alias
An archetype represents a specific set of component types, and an alias is simply a name for that specific archetype. 

For example, say in our game we had the following components defined:
- PositionComponent
- RotationComponent
- CircleColliderComponent
- PhysicsComponent

We may define an alias called 'PhysicsBall' which contains all 4 of those components. This simplifies our query system, as now we may have a system which simply queries world for all entities which match the 'PhysicsBall' alias.

### Inputs
Inputs are a special type of alias'd archetype that is passed in each game tick. This state represents the external influence of the player on the simulation. Each game frame is always the result of taking the previous game state in addition to the inputs for the next frame. Assuming those 2 things are not changed, the next frame state will be the same.


## Getting Started

### Defining Our Game State
In order to get started, we first need to define the state data that will be used in our game. All of this data will be defined in a set of '.schema' files located in a specific folder. We will then run our code generator on this set of schema files, producing a bunch of game-specific implementations of interfaces our core game library will use to run our game.

Technically all the game state can be defined in a single schema file, but we will break it up into 3 for organizational purposes:
- Core Game State: The component data that will fully define each game frame's state at the end of each tick
- Inputs: The compnoent data that will be consumed on each tick to affect the resolution of that tick
- Aliases: Groupings of components that can be given an alias, for convenience.
  
For a full detailed explanation of schema types and attributes, see [IndigoECS Code Generator](https://github.com/rdshack/ecs_gen/tree/main). 

For now, we'll just give some simple example schemas for a make-believe game. Let's say we have a simple 2d billiards simulation with some balls we want to bump around, and some static geometry (walls). We might have some game state that looks like this:

core_game_state.schema:
```
enum CollisionLayer
{
  Ball,
  StaticGeo
}

component PositionComponent
{  
  pos: Fix64Vec2;
}

component RotationComponent
{  
  rot: Fix64;
}

component VelocityComponent
{  
  velo: Fix64Vec2;
  maxSpeed : Fix64;
}

component RotationalVelocityComponent
{  
  rotVelo: Fix64;
}

component CircleColliderComponent
{  
  radius : int;
  collisionLayer : CollisionLayer;
}

component BoxColliderComponent
{  
  width: int;
  height: int;
  collisionLayer: CollisionLayer;
}

component BallPhysicsComponent
{
  drag : int;
  angularDrag : int;
  mass : int;
}

//Note that 'Fix64' is our replacement for 'float'. This is important to guarantee determinism, which is needed if your game ever uses the state-rollback feature.
//'Fix64Vec2' represents a 2d vector of the same type.
```

We can then create 2 aliases for our "Ball" object, and our "Wall" Object.

alias.schema:
```
alias Ball
{  
  PositionComponent,
  VelocityComponent,
  RotationComponent,
  RotationalVelocityComponent,
  CircleColliderComponent,
  BallPhysicsComponent
}

alias StaticGeo
{  
  PositionComponent,
  RotationComponent,
  BoxColliderComponent
}

```

While Balls and StaticGeo share some component state (positions, rotations), they are otherwise different. Cool, so we've got our core game objects defined. Next we need to define our inputs.

This isn't really a game, but let's say we want to be able to affect the world with 2 input types:
- Spawn Ball: Spawns a new ball at position, with starting velocity
- Radial Force: Applies a force radially to everything around target position

inputs.schema:
```
component PlayerOwnedComponent
{  
  [key]
  playerId: int;
}

component SpawnBallComponent
{  
  pos: Fix64Vec2;
  startVeloX: Fix64Vec2;
}

component RadialForceComponent
{  
  pos: Fix64Vec2;
  forceDirection: Fix64Vec2;
  forceToApply: Fix64;
}
```
You'll nootice that we attached a 'key' attribute to our 'PlayerOwnedComponent' playerId field. Without getting into the weeds too much, this is needed to tell the framework how to tell where the inputs came from (which player). The key field doesn't have to be an integer, it can be any type that can be compared for equality.

We can then create 2 aliases for our inputs:

alias.schema:
```
[input(PlayerOwnedComponent)]
alias AddBallInput
{
  PlayerOwnedComponent,
  SpawnBallComponent
}

[input(PlayerOwnedComponent)]
alias RadialForceInput
{
  PlayerOwnedComponent,
  RadialForceComponent
}

```

Notice that we tell the generator that 'PlayerOwnedComponent' is the key-component for that input. That is to say, that is the component that determined where the input came from. All input aliases must be defined with the same key-component. In the future I'll probably clean this up to just be defined in a single line somewhere else (as forcing its declaration on each input alias is redundant).

### Generate Code From Schemas

Now that we've created all the schema files we'll be using for our small demo, we can run our code generator against those files. See [IndigoECS Code Generator](https://github.com/rdshack/ecs_gen/tree/main) for how to run the code generator.

After we've run the generator, your target output folder should look something like this:

<img src="https://i.imgur.com/mikK2u0.png">

The **Component** folder contains all your C# component data classes, and the **FlatBuffers** folder contains the code generated by Google Flatbuffers serializer build tool.

### Building our Systems

TODO

### Constructing Your First World

TODO

### Running The Game

TODO

