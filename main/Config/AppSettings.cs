namespace Config;

// Комментарий: класс для загрузки конфигурации JWT из appsettings.json
public class JwtSettings //JSON Web Token
{
    public string SecretKey { get; set; } = null!; // Секретный ключ для подписи токена
    public string Issuer { get; set; } = null!; // Издатель токена
    public string Audience { get; set; } = null!; // Аудитория токена
}
