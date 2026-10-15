namespace Zyft.Domain.Profiles;

public enum WorkModel { OnSite = 1, Hybrid = 2, Remote = 3 }

public enum EducationLevel
{
    ElementarySchool = 1,
    HighSchool = 2,
    Technical = 3,
    Undergraduate = 4,
    Postgraduate = 5,
    Masters = 6,
    Doctorate = 7,
}

public enum EducationStatus { InProgress = 1, Completed = 2, Paused = 3, Incomplete = 4 }

public enum CourseType { Course = 1, Certification = 2 }

public enum SkillLevel { Basic = 1, Intermediate = 2, Advanced = 3 }

public enum LanguageProficiency { Basic = 1, Intermediate = 2, Advanced = 3, Fluent = 4, Native = 5 }
