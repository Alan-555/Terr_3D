using OpenTK.Mathematics;
using Terr3D.Server.Core;

namespace Terr3D.Server.Entities;

/// <summary>
/// An empty entity which can be instantiated
/// </summary>
public class EmptyEntity(string name, Entity parent) : Entity(name, parent);