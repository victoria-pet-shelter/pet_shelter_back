import uuid
import hashlib

# Фиктивная база данных
users_db = {}

def hash_password(password):
    salt = uuid.uuid4().hex  # Генерируем случайную соль
    return hashlib.sha256(salt.encode() + password.encode()).hexdigest() + ':' + salt

def check_password(hashed_password, user_password):
    password, salt = hashed_password.split(':')
    return password == hashlib.sha256(salt.encode() + user_password.encode()).hexdigest()

def register():
    print("\n--- Регистрация ---")
    email = input("Введите email: ")
    if email in users_db:
        print("Пользователь с таким email уже существует!")
        return
    
    role = input("Выберите роль (client/shelter): ").lower()
    if role not in ["client", "shelter"]:
        print("Некорректная роль. Выберите 'client' или 'shelter'.")
        return

    password = input("Введите пароль: ")
    hashed_password = hash_password(password)

    # Сохраняем пользователя в базе данных
    users_db[email] = {
        "role": role,
        "password": hashed_password
    }
    print(f"Регистрация успешна! Вы зарегистрировались как {role}.")

def login():
    print("\n--- Вход ---")
    email = input("Введите email: ")
    if email not in users_db:
        print("Пользователь с таким email не найден!")
        return

    password = input("Введите пароль: ")
    if check_password(users_db[email]["password"], password):
        print(f"Вход успешен! Добро пожаловать, {email}. Ваша роль: {users_db[email]['role']}.")
    else:
        print("Неверный пароль!")

def main():
    while True:
        print("\n--- Главное меню ---")
        print("1. Регистрация")
        print("2. Вход")
        print("3. Выход")

        choice = input("Выберите действие (1/2/3): ")
        if choice == "1":
            register()
        elif choice == "2":
            login()
        elif choice == "3":
            print("До свидания!")
            break
        else:
            print("Некорректный выбор. Попробуйте снова.")

if __name__ == "__main__":
    main()


