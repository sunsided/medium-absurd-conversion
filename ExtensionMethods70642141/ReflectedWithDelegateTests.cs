using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;

namespace ExtensionMethods70642141;

public class ReflectedWithDelegateTests
{
    [Fact]
    public void Reflected()
    {
        var student = new Student { Name = "Student Name" };
        var teacher = new Teacher { Name = "Teacher Name" };

        var studentDto = ConvertToDTO<StudentDTO, Student>(student);
        var student2Dto = ConvertToDTO<StudentDTO, Student>(student);
        var teacherDto = ConvertToDTO<TeacherDTO, Teacher>(teacher);

        Assert.Equal(student.Name, studentDto.Name);
        Assert.Equal(student.Name, student2Dto.Name);
        Assert.Equal(teacher.Name, teacherDto.Name);
    }

    [Fact]
    public void InvalidConversionFails()
    {
        var student = new Student { Name = "Student Name" };

        var invalidCall = () => ConvertToDTO<TeacherDTO, Student>(student);

        Assert.Throws<InvalidOperationException>(invalidCall);
    }

    private static readonly ConcurrentDictionary<Type, Func<object, object>> _cache = new();

    private TDTO ConvertToDTO<TDTO, TData>(TData data)
    {
        var inputType = typeof(TData);
        var outputType = typeof(TDTO);
        if (_cache.TryGetValue(inputType, out var toDto))
        {
            return (TDTO)toDto(data!);
        }

        var methodInfo = GetMatchingMethodInfo(outputType, inputType);
        if (methodInfo is null)
        {
            throw new InvalidOperationException($"No conversion from {inputType} to {outputType} was registered");
        }

        toDto = CompileLambda(inputType, methodInfo);
        _cache.TryAdd(inputType, toDto);

        return (TDTO)toDto(data!);
    }

    private static Func<object, object> CompileLambda(Type inputType, MethodInfo methodInfo)
    {
        var inputObject = Expression.Parameter(typeof(object), "dataObj");
        var inputCastToProperType = Expression.Convert(inputObject, inputType);
        var callExpr = Expression.Call(null, methodInfo, inputCastToProperType);
        var castResultExpr = Expression.Convert(callExpr, typeof(object));
        var lambdaExpr = Expression.Lambda<Func<object, object>>(castResultExpr, inputObject);
        return lambdaExpr.Compile();
    }

    private static MethodInfo? GetMatchingMethodInfo(Type outputType, Type inputType) =>
        typeof(PeopleExtension)
            .GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(method => method.Name.Equals(nameof(PeopleExtension.ToDTO)))
            .Where(method => outputType == method.ReturnType)
            .FirstOrDefault(method => inputType == method.GetParameters().SingleOrDefault()?.ParameterType);
}
