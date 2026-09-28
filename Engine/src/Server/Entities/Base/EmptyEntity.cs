using OpenTK.Mathematics;
using Terr3D.Server.Engine;

namespace Terr3D.Server.Entities;

/// <summary>
/// An empty entity which can be instantiated
/// </summary>
public class EmptyEntity(string name, Entity parent, bool isStatic = false) : Entity(name, parent, isStatic);