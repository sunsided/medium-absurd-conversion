using AutoMapper;
using Xunit;

namespace ExtensionMethods70642141;

public class AutomapperTests
{
    private readonly IMapper _mapper;

    public AutomapperTests()
    {
        var configuration = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<Student, StudentDTO>();
            cfg.CreateMap<Teacher, TeacherDTO>();
        });

#if DEBUG
        configuration.AssertConfigurationIsValid();
#endif

        _mapper = configuration.CreateMapper();
    }

    [Fact]
    public void Automapper()
    {
        var student = new Student { Name = "Student Name" };
        var teacher = new Teacher { Name = "Teacher Name" };

        var studentDto = _mapper.Map<StudentDTO>(student);
        var teacherDto = _mapper.Map<TeacherDTO>(teacher);

        Assert.Equal(student.Name, studentDto.Name);
        Assert.Equal(teacher.Name, teacherDto.Name);
    }

    [Fact]
    public void AutomapperFails()
    {
        var student = new Student { Name = "Student Name" };

        var invalidCall = () => _mapper.Map<TeacherDTO>(student);

        Assert.Throws<AutoMapperMappingException>(invalidCall);
    }
}
