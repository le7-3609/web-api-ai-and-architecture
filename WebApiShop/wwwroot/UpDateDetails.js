const text = document.querySelector(".text")
let oldUser = JSON.parse(sessionStorage.getItem("User"))
const updatedUserOpen = document.querySelector(".open")
const updatedUserBox = document.querySelector(".updatedUser")

if (!oldUser) {
    window.location.href = "index.html";
}

text.textContent = `שלום ${oldUser.firstName}`

 function toggleUpdate() {
    const updatedUserDiv = document.querySelector('.updatedUser');
    if (updatedUserDiv.style.display === 'flex') {
        updatedUserDiv.style.display = 'none';
    } else {
        updatedUserDiv.style.display = 'flex';
        updatedUserDiv.style.flexDirection = 'column';
    }
 }

updatedUserOpen.addEventListener("click", e => {
    updatedUserOpen.style.color = "red"
    updatedUserBox.style.display = "flex"
})

const getDataFromForm = () => {
    const updatedUser = {
        Email: document.querySelector("#userName").value || oldUser.email,
        FirstName: document.querySelector("#firstName").value || oldUser.firstName,
        LastName: document.querySelector("#lastName").value || oldUser.lastName,
        Phone: document.querySelector("#phone").value || oldUser.phone,
        Password: document.querySelector("#password").value || oldUser.password
    }
    return updatedUser
}

const updateUserDetails = async () => {
    const updatedUser = getDataFromForm();
    try {
        const responsePost = await fetch(`/api/users/${oldUser.userId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(updatedUser)

        });
        if (responsePost.status == 404) {
            alert("העדכון נכשל: משתמש לא נמצא, סיסמה חלשה או שהמייל כבר תפוס")
        }
        else if (responsePost.status == 400) {
            alert("נתונים לא תקינים, בדוק/י את שדות הטופס")
        }
        else if (!responsePost.ok) {
            throw new Error(`HTTP error! status:${responsePost.status}`)
        }
        else {
            const serverUser = await responsePost.json();
            sessionStorage.setItem("User", JSON.stringify(serverUser))
            alert("הפרטים עודכנו בהצלחה")
        }
    }
    catch (error) {
        console.log(error)
    }
}