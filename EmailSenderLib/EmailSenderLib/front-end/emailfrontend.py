import streamlit as st
import requests

# Streamlit frontend
st.title("Email Sender Frontend")

# Input fields for 'To', 'Subject', and 'Body'
to_email = st.text_input("To (Recipient Email Address)")
subject = st.text_input("Subject")
body = st.text_area("Body")

# Submit button
if st.button("Send Email"):
    # Create payload for the API
    payload = {
        "to": to_email,
        "subject": subject,
        "body": body
    }

    # Send request to the EmailSenderAPI
    try:
        response = requests.post("https://your_backend_localhost/api/Email/send", json=payload, verify=False)

        if response.status_code == 200:
            st.success("Email sent successfully!")
        else:
            st.error(f"Failed to send email. Status code: {response.status_code}")
    except Exception as e:
        st.error(f"An error occurred: {e}")
