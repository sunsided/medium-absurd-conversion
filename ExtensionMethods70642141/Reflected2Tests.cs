using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Xunit;

namespace ExtensionMethods70642141;

public class Reflected2Tests
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

    private static readonly ConcurrentDictionary<Tuple<Type, Type>, MethodInfo> _cache = new();

    private TDTO ConvertToDTO<TDTO, TData>(TData data)
    {
        if (_cache.IsEmpty)
        {
            var methodInfos = typeof(PeopleExtension)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(method => method.Name.Equals(nameof(PeopleExtension.ToDTO)))
                .Where(method => method.GetParameters().Length == 1);

            foreach (var info in methodInfos)
            {
                var key = Tuple.Create(info.GetParameters().Single().ParameterType, info.ReturnType);
                _cache.TryAdd(key, info);
            }

            if (_cache.IsEmpty)
            {
                throw new InvalidOperationException("No conversion methods could be found using reflection");
            }
        }

        var cacheKey = Tuple.Create(typeof(TData), typeof(TDTO));
        if (!_cache.TryGetValue(cacheKey, out var methodInfo))
        {
            throw new InvalidOperationException($"No conversion from {typeof(TData)} to {typeof(TDTO)} was registered");
        }

        return (TDTO)methodInfo.Invoke(null, new object?[] { data })!;
    }
}
