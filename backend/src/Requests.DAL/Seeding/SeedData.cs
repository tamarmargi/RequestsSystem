namespace Requests.DAL.Seeding;

/// <summary>Static source lists for test data generation. Order matters: generation is deterministic.</summary>
public static class SeedData
{
    private static readonly string[] Cities =
    {
        "ירושלים", "תל אביב-יפו", "חיפה", "ראשון לציון", "פתח תקווה", "אשדוד", "נתניה", "באר שבע",
        "בני ברק", "חולון", "רמת גן", "אשקלון", "רחובות", "בת ים", "בית שמש", "כפר סבא",
        "הרצליה", "חדרה", "מודיעין-מכבים-רעות", "נצרת", "לוד", "רמלה", "רעננה", "רהט",
        "הוד השרון", "גבעתיים", "קריית גת", "נהריה", "עפולה", "אילת", "קריית אתא", "עכו",
        "אום אל-פחם", "קריית מוצקין", "יבנה", "ראש העין", "טבריה", "דימונה", "צפת", "אור יהודה"
    };

    private static readonly string[] RegionalCouncils =
    {
        "גליל עליון", "עמק יזרעאל", "חוף הכרמל", "מטה יהודה", "שער הנגב", "אשכול", "גזר", "דרום השרון",
        "לב השרון", "עמק הירדן", "מגידו", "הגלבוע", "חבל מודיעין", "באר טוביה", "רמת נגב"
    };

    private static readonly string[] Hospitals =
    {
        "המרכז הרפואי שיבא", "המרכז הרפואי סוראסקי", "הדסה עין כרם", "המרכז הרפואי רמב\"ם",
        "המרכז הרפואי סורוקה", "המרכז הרפואי רבין - בילינסון", "המרכז הרפואי שערי צדק", "המרכז הרפואי מאיר",
        "המרכז הרפואי וולפסון", "המרכז הרפואי אסף הרופא", "המרכז הרפואי ברזילי", "המרכז הרפואי העמק",
        "המרכז הרפואי הלל יפה", "המרכז הרפואי לגליל", "המרכז הרפואי זיו", "המרכז הרפואי קפלן",
        "Laniado Medical Center", "Assuta Medical Centers", "Bnai Zion Medical Center", "Carmel Medical Center"
    };

    private static readonly string[] Ngos =
    {
        "עמותת לתת", "עמותת עזר מציון", "עמותת אלו\"ט", "עמותת אקים", "עמותת יד שרה", "עמותת לב חם",
        "עמותת ידידים", "עמותת שלום כיתה א'", "עמותת עלם", "עמותת אחת משלנו", "עמותת קרן אור", "עמותת בית חם",
        "עמותת מעגלי צדק", "עמותת חברים לרפואה", "עמותת צעד קדימה", "עמותת גשר לעתיד", "עמותת אור ירוק",
        "Israel Nature Fund", "Friends of the Negev", "Green Coast Association", "Open Doors Israel",
        "Youth Futures Israel", "Hope for Families", "Clean Water Israel", "Galilee Education Network"
    };

    private static readonly string[] HebrewCompanyNames =
    {
        "אלון", "ברקת", "גפן", "דקל", "הדר", "ורד", "זית", "חרמון", "טל", "ים המלח", "כרמל", "לבונה", "מצדה",
        "נגב", "סלע", "ערבה", "פרי הגליל", "צור", "קשת", "רימון", "שקד", "תבור", "אורן", "ארז", "גלבוע"
    };

    private static readonly string[] HebrewCompanyTypes =
    {
        "תעשיות בע\"מ", "שירותים בע\"מ", "טכנולוגיות בע\"מ", "השקעות בע\"מ"
    };

    private static readonly string[] EnglishCompanyNames =
    {
        "Orion", "Negev", "Jordan Valley", "Blue Horizon", "Cedar", "Galil", "Sharon", "Sapphire", "Arava",
        "Northstar", "Mediterranean", "Desert Rose", "Lighthouse", "Silverline", "Golan", "Harbor", "Zenith",
        "Olive Tree", "Red Sea", "Summit", "Pioneer", "Keystone", "Bluebird", "Evergreen", "Crescent"
    };

    private static readonly string[] EnglishCompanyTypes =
    {
        "Technologies Ltd", "Systems Ltd", "Logistics Ltd", "Group"
    };

    /// <summary>300 distinct organizations: municipalities, regional councils, hospitals, NGOs and companies.</summary>
    public static readonly IReadOnlyList<string> Organizations = BuildOrganizations();

    /// <summary>50 handlers used as AssignedTo / ChangedBy.</summary>
    public static readonly IReadOnlyList<string> Handlers = new[]
    {
        "יוסי כהן", "מיכל לוי", "דוד מזרחי", "שרה פרץ", "אבי ביטון", "רחל אברהם", "משה פרידמן", "נועה שפירא",
        "איתי דהן", "תמר אזולאי", "עומר גולן", "יעל ברק", "רונן אוחיון", "הילה סגל", "אורי חדד", "ליאת קפלן",
        "גיל רוזן", "מירב שטרן", "עידו בן דוד", "שירה וקנין", "אלון גבאי", "דנה מלכה", "יונתן אשכנזי", "אסתר עמר",
        "נדב יוסף", "רוני טל", "אריאל שלום", "ענבל כץ", "אייל נחום", "קרן אלמוג", "שי רבינוביץ", "מאיה זילבר",
        "בועז חזן", "ליה אלון", "טל גרינברג", "אורית בירנבאום", "עמית ששון", "נטע הורוביץ", "רועי לנדאו", "חן עזרא",
        "אליעזר מור", "דפנה שמעוני", "ניר אדלר", "גלית ברגר", "זיו פישר", "אפרת סויסה", "יגאל קרמר", "סיגל נגר",
        "אהרון וייס", "רבקה ליבוביץ"
    };

    /// <summary>40 Hebrew request title templates.</summary>
    public static readonly IReadOnlyList<string> TitleTemplates = new[]
    {
        "בקשה לעדכון פרטי עובד", "תלונה על עיכוב בתשלום", "בקשה לאישור חריגה", "בקשה להנפקת אישור",
        "בקשה לשינוי כתובת", "בקשה להחזר כספי", "תלונה על שירות לקוחות", "בקשה לפגישה עם נציג",
        "בקשה לקבלת מסמכים", "עדכון פרטי חשבון בנק", "בקשה לפריסת תשלומים", "ערעור על החלטה",
        "בקשה להארכת מועד הגשה", "בקשה לביטול הזמנה", "דיווח על תקלה במערכת", "בקשה להרשאת גישה",
        "בקשה לאישור תקציב", "בקשה לרכש ציוד", "תלונה על רעש", "בקשה לפינוי פסולת",
        "בקשה לרישיון עסק", "בקשה להיתר בנייה", "בקשה לבדיקת חשבונית", "עדכון אנשי קשר",
        "בקשה לקבלת הצעת מחיר", "בקשה להעברת בעלות", "תלונה על איכות מוצר", "בקשה לתיקון שגיאה בחשבון",
        "בקשה לאישור העסקת עובד", "בקשה לשחרור ערבות", "בקשה למידע לפי חוק חופש המידע", "בקשה לסגירת תיק",
        "בקשה לפתיחת תיק חדש", "בקשה לחידוש חוזה", "בקשה לאישור נסיעה", "בקשה להכרה בהוצאות",
        "דיווח על מפגע בטיחותי", "בקשה לשיבוץ מחדש", "בקשה לאישור תוכנית עבודה", "בקשה לבדיקה חוזרת"
    };

    /// <summary>Optional suffixes appended to some titles so they vary.</summary>
    public static readonly IReadOnlyList<string> TitleSuffixes = new[]
    {
        "דחוף", "המשך טיפול", "פנייה חוזרת", "רבעון ראשון", "רבעון שני", "רבעון שלישי", "רבעון רביעי",
        "סניף מרכז", "סניף צפון", "סניף דרום", "מחלקת כספים", "משאבי אנוש"
    };

    private static IReadOnlyList<string> BuildOrganizations()
    {
        var organizations = new List<string>(300);
        organizations.AddRange(Cities.Select(city => $"עיריית {city}"));
        organizations.AddRange(RegionalCouncils.Select(name => $"מועצה אזורית {name}"));
        organizations.AddRange(Hospitals);
        organizations.AddRange(Ngos);
        organizations.AddRange(HebrewCompanyNames.SelectMany(_ => HebrewCompanyTypes, (name, type) => $"{name} {type}"));
        organizations.AddRange(EnglishCompanyNames.SelectMany(_ => EnglishCompanyTypes, (name, type) => $"{name} {type}"));
        return organizations;
    }
}
