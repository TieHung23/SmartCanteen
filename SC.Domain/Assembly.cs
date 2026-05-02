using System;

namespace SC.Domain;

public static class Assembly
{
    public static System.Reflection.Assembly Get => typeof(Assembly).Assembly;
}
