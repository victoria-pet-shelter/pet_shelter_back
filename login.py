import uuid
import hashlib

# Фиктивная база данных
users_db = {}

def hash_password(password):
    salt = uuid.uuid4().hex  # generating salt
    return hashlib.sha256(salt.encode() + password.encode()).hexdigest() + ':' + salt

def check_password(hashed_password, user_password):
    password, salt = hashed_password.split(':')
    return password == hashlib.sha256(salt.encode() + user_password.encode()).hexdigest()

def register():
    print("\n--- Registration ---")
    email = input("Enter an e-mail: ")
    if email in users_db:
        print("User with provided e-mail already exists")
        return
    
    role = input("Select role (client/shelter): ").lower()
    if role not in ["client", "shelter"]:
        print("Incorrect role. Select 'client' or 'shelter'.")
        return

    password = input("Enter password: ")
    hashed_password = hash_password(password)

    # Сохраняем пользователя в базе данных
    users_db[email] = {
        "role": role,
        "password": hashed_password
    }
    print(f"Registration successful. You have been assigned the {role} role.")

def login():
    print("\n--- Login ---")
    email = input("Enter an e-mail: ")
    if email not in users_db:
        print("User with provided e-mail does not exist")
        return

    password = input("Enter password: ")
    if check_password(users_db[email]["password"], password):
        print(f"Login successful. Welcome, {email}. Your role is {users_db[email]['role']}.")
    else:
        print("Incorrect password")

def main():
    while True:
        print("\n--- Main menu ---")
        print("1. Registration")
        print("2. Login")
        print("3. Exit")

        choice = input("Select an item (1/2/3): ")
        if choice == "1":
            register()
        elif choice == "2":
            login()
        elif choice == "3":
            print("Goodbye.")
            break
        else:
            print("Incorrect item. Please try again.")

if __name__ == "__main__":
    main()