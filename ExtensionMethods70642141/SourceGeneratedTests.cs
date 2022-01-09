using System;
using Xunit;

namespace ExtensionMethods70642141;

public class SourceGeneratedTests
{
    [Fact]
    public void Works()
    {
        var student = new Student { Name = "Student Name" };
        var teacher = new Teacher { Name = "Teacher Name" };

        // var studentDto = GenericPeopleConversion.ToDTO<StudentDTO, Student>(student);
        // var teacherDto = GenericPeopleConversion.ToDTO<TeacherDTO, Teacher>(teacher);

        //Assert.Equal(student.Name, studentDto.Name);
        // Assert.Equal(teacher.Name, teacherDto.Name);
    }

    [Fact]
    public void InvalidConversionFails()
    {
        var student = new Student { Name = "Student Name" };

        var invalidCall = () => GenericPeopleConversion.ToDTO<TeacherDTO, Student>(student);

        Assert.Throws<InvalidOperationException>(invalidCall);
    }
}
