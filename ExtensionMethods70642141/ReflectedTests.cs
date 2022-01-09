using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Xunit;

namespace ExtensionMethods70642141;

public class ReflectedTests
{
    [Fact]
    public void Reflected()
    {
        var student = new Student { Name = "Student Name" };
        var teacher = new Teacher { Name = "Teacher Name" };

        var studentDto = ToDTO<StudentDTO, Student>(student);
        var student2Dto = ToDTO<StudentDTO, Student>(student);
        var teacherDto = ToDTO<TeacherDTO, Teacher>(teacher);

        Assert.Equal(student.Name, studentDto.Name);
        Assert.Equal(student.Name, student2Dto.Name);
        Assert.Equal(teacher.Name, teacherDto.Name);
    }

    [Fact]
    public void InvalidConversionFails()
    {
        var student = new Student { Name = "Student Name" };

        var invalidCall = () => ToDTO<TeacherDTO, Student>(student);

        Assert.Throws<InvalidOperationException>(invalidCall);
    }

    private static readonly ConcurrentDictionary<Type, MethodInfo> _cache = new();

    private TDTO ToDTO<TDTO, TData>(TData data)
    {
        var inputType = typeof(TData);
        var outputType = typeof(TDTO);
        if (!_cache.TryGetValue(inputType, out var methodInfo))
        {
            methodInfo = GetMatchingMethodInfo(outputType, inputType);
            if (methodInfo is null)
            {
                throw new InvalidOperationException($"No conversion from {inputType} to {outputType} was registered");
            }

            _cache.TryAdd(inputType, methodInfo);
        }

        return (TDTO)methodInfo.Invoke(null, new object?[] { data })!;
    }

    private static MethodInfo? GetMatchingMethodInfo(Type outputType, Type inputType) =>
        typeof(PeopleExtension)
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(method => method.Name.Equals(nameof(PeopleExtension.ToDTO)))
            .Where(method => outputType == method.ReturnType)
            .FirstOrDefault(method => inputType == method.GetParameters().SingleOrDefault()?.ParameterType);
}
