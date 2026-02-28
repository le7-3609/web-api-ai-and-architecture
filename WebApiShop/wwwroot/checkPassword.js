
const colorTheBar = (dataPost) => {
    console.log(dataPost)
    const bar = document.querySelector('.bar');
    const checkBtn = document.getElementById('checkBtn');
    bar.style.display = "block";
    bar.value = dataPost.strength * 25;
}

async function checkPassword() {
    const password = document.querySelector("#password").value
    try {
        const responsePost = await fetch('/api/passwordvalidity/passwordstrength', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(password)
        });
        if (!responsePost.ok) {
            throw new Error(`HTTP error! status:${responsePost.status}`)
        }
        const dataPost = await responsePost.json();
        colorTheBar(dataPost)
        return dataPost;
    }
    catch (e) {
        console.log(e)
    }
};

