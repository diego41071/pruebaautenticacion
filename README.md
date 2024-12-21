# README - API de Autenticación con LDAP

## **Configuración Inicial**

### **Requisitos Previos**

1. **SDK de .NET**: Asegúrate de tener instalado el SDK de .NET (versión 6 o superior).

2. **Docker**: Necesario para ejecutar el servidor LDAP simulado.

3. **Herramientas Adicionales**:
   - Postman: Para realizar pruebas de los endpoints.
   - Editor de texto o IDE como Visual Studio 2022.

### **Configuración del Archivo `appsettings.json`**

Asegúrate de tener el archivo `appsettings.json` configurado correctamente:

```json
{
  "LDAP": {
    "Server": "ldap://localhost",
    "Port": 389,
    "BaseDn": "dc=example,dc=com",
    "UserFormat": "uid={username},ou=users,dc=example,dc=com"
  },
  "Jwt": {
    "Key": "YourSecretKey",
    "Issuer": "YourIssuer",
    "Audience": "YourAudience"
  }
}
```

### **Instalación de Dependencias**

1. Restaura los paquetes NuGet necesarios:
   ```bash
   dotnet restore
   ```
2. Verifica que las dependencias están instaladas correctamente, incluyendo:
   - `Microsoft.AspNetCore.Authentication.JwtBearer`
   - `Novell.Directory.Ldap`
   - `System.DirectoryServices.Protocols`

---

## **Configuración de un Servidor LDAP**

### **Opción 1: Usar un Contenedor Docker con OpenLDAP**

Ejecuta el siguiente comando para iniciar un servidor LDAP simulado con Docker:

```bash
docker run --name openldap -p 389:389 -e LDAP_ORGANISATION="Example Org" \
-e LDAP_DOMAIN="example.com" -e LDAP_ADMIN_PASSWORD="admin" -d osixia/openldap
```

#### **Agregar Usuarios al Servidor**

1. Crea un archivo llamado `users.ldif` con el siguiente contenido:

   ```ldif
   dn: ou=users,dc=example,dc=com
   objectClass: organizationalUnit
   ou: users

   dn: uid=jdoe,ou=users,dc=example,dc=com
   objectClass: inetOrgPerson
   cn: John Doe
   sn: Doe
   uid: jdoe
   mail: jdoe@example.com
   userPassword: password123
   ```

2. Importa los usuarios al servidor LDAP:
   ```bash
   docker cp users.ldif openldap:/users.ldif
   docker exec -it openldap ldapadd -x -D "cn=admin,dc=example,dc=com" -w admin -f /users.ldif
   ```

### **Opción 2: Configurar Active Directory (AD)**

1. Instala y configura Active Directory en un servidor Windows.
2. Crea un usuario en el dominio y asegúrate de que el formato del usuario sea compatible (e.g., `user@domain.com`).
3. Ajusta los valores de `BaseDn` y `UserFormat` en `appsettings.json`.

---

## **Comandos para Ejecutar la API**

### **Ejecutar en Desarrollo**

1. Inicia la API:

   ```bash
   dotnet run
   ```

2. La API estará disponible en:
   - `http://localhost:7243`
   - `https://localhost:7243`

### **Prueba con Postman**

- Endpoint de autenticación:
  - URL: `POST http://localhost:7243/api/authenticate`
  - Body (JSON):
    ```json
    {
      "username": "jdoe",
      "password": "password123"
    }
    ```
- Respuesta exitosa:
  ```json
  {
    "status": "success",
    "user": {
      "username": "jdoe",
      "displayName": "John Doe",
      "email": "jdoe@example.com",
      "department": "IT",
      "title": "Software Engineer"
    }
  }
  ```

### **Crear el Contenedor Docker para la API**

1. Crea una imagen Docker para la API:

   ```bash
   docker build -t openldap .
   ```

2. Ejecuta el contenedor:
   Por medio de docker desktop ejecutar el contenedor openldap

---

## **Notas Finales**

- Asegúrate de que el servidor LDAP esté corriendo antes de probar la API.
- Revisa los logs del servidor LDAP para solucionar problemas de conexión o autenticación.
- Configura una clave JWT segura en producción para proteger los tokens generados.
