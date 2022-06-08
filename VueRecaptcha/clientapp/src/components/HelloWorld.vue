<template>
  <div class="hello">
    <div v-if="uploaded">Файлы загружены</div>
    <form method="POST" @submit.prevent="" class="uploadForm" v-else>
      <label for="email"
        >Email
        <input type="email" id="email" name="email" required v-model="email" />
      </label>
      <label for="name"
        >Name
        <input type="text" id="name" name="name" required v-model="name" />
      </label>
      <input type="file" multiple hidden id="upload_files" />
      <div class="g-recaptcha" :data-sitekey="siteKey"></div>
      <button @click="addFiles()">Add files</button>

      <button type="submit" @click.prevent="submit">Submit</button>
    </form>
  </div>
</template>
<script lang="ts">
import { Options, Vue } from "vue-class-component";
import axios from "axios";
@Options({
  props: {
    msg: String,
  },
})
export default class HelloWorld extends Vue {
  msg!: string;
  siteKey: string = "";
  email: string = "";
  name: string = "";
  uploaded: boolean = false;
  async submit() {
    // console.log(ev.srcElement);
    var formData = new FormData();
    formData.append("email", this.email);
    formData.append("name", this.name);
    const resp = (
      document.getElementsByName(
        "g-recaptcha-response"
      )[0] as HTMLTextAreaElement
    ).value;
    formData.append("captchaResponse", resp);
    var imagefile = document.getElementById("upload_files") as HTMLInputElement;
    if (imagefile == null) return;
    if (imagefile.files == null) return;
    const files = imagefile.files;
    for (let i = 0; i < files.length; i++) {
      formData.append("files", files[i]);
    }
    var result = await axios.post("/api/upload", formData);
    if (result) {
      console.log(result);
      this.uploaded = true;
    }
    return false;
  }
  addFiles() {
    const upload = document.getElementById("upload_files");
    upload?.click();
    return false;
  }
  async mounted() {
    var captcha = await axios.get("/api/upload");
    this.siteKey = captcha.data.toString();
    this.createRecaptcha();
  }
  createRecaptcha() {
    var s = document.createElement("script");
    s.setAttribute("src", "https://www.google.com/recaptcha/api.js");
    s.setAttribute("async", "async");
    s.setAttribute("defer", "defer");
    document.body.appendChild(s);
  }
}
</script>

<!-- Add "scoped" attribute to limit CSS to this component only -->
<style scoped>
a {
  color: #42b983;
}
.uploadForm {
  margin: 20px auto;
  width: 300px;
  display: block;
}
.uploadForm label {
  display: block;
  width: 100%;
  margin-bottom: 10px;
}
</style>
