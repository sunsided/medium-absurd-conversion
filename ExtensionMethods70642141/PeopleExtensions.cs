namespace ExtensionMethods70642141;

public static class PeopleExtension
{
    public static StudentDTO ToDTO(this Student student) => new()
    {
        Name = student.Name
    };

    public static TeacherDTO ToDTO(this Teacher teacher) => new()
    {
        Name = teacher.Name
    };
}
