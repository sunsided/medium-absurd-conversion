using Xunit;

namespace ExtensionMethods70642141;

public class SmokeTests
{
    [Fact]
    public void Smoke()
    {
        var student = new Student { Name = "Student Name" };
        var teacher = new Teacher { Name = "Teacher Name" };

        var studentDto = student.ToDTO();
        var teacherDto = teacher.ToDTO();

        Assert.Equal(student.Name, studentDto.Name);
        Assert.Equal(teacher.Name, teacherDto.Name);
    }
}
