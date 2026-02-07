#!/usr/bin/env python3
"""
End-to-End OAuth2/XOAUTH2 Authentication Test with Keycloak

This script demonstrates the complete OAuth2 flow:
1. Obtains a real JWT access token from Keycloak
2. Authenticates to smtp4dev using XOAUTH2 with the token
3. smtp4dev validates the token against Keycloak
4. Sends an email
5. Verifies the email was received
"""

import base64
import json
import smtplib
import sys
import time
from email.mime.text import MIMEText
from email.mime.multipart import MIMEMultipart

try:
    import requests
except ImportError:
    print("Installing requests...")
    import subprocess
    subprocess.check_call([sys.executable, "-m", "pip", "install", "-q", "requests"])
    import requests


class OAuth2E2ETest:
    def __init__(self):
        # Keycloak configuration
        self.keycloak_url = "http://localhost:8080"
        self.realm = "smtp4dev"
        self.client_id = "smtp4dev-client"
        self.client_secret = "smtp4dev-secret"
        self.username = "testuser"
        self.password = "testpass"
        
        # smtp4dev configuration
        self.smtp_host = "localhost"
        self.smtp_port = 2525
        self.api_url = "http://localhost:5000/api"
        
    def get_oauth2_token(self):
        """Obtain a real OAuth2 access token from Keycloak"""
        print("\n" + "="*70)
        print("STEP 1: Obtaining OAuth2 Access Token from Keycloak")
        print("="*70)
        
        token_url = f"{self.keycloak_url}/realms/{self.realm}/protocol/openid-connect/token"
        
        data = {
            'grant_type': 'password',
            'client_id': self.client_id,
            'client_secret': self.client_secret,
            'username': self.username,
            'password': self.password,
            'scope': 'openid profile email'
        }
        
        print(f"Token endpoint: {token_url}")
        print(f"Client ID: {self.client_id}")
        print(f"Username: {self.username}")
        print(f"Grant type: password")
        
        try:
            response = requests.post(token_url, data=data, timeout=10)
            response.raise_for_status()
            
            token_response = response.json()
            access_token = token_response['access_token']
            
            print(f"\n✓ Token obtained successfully!")
            print(f"  Token type: {token_response.get('token_type', 'N/A')}")
            print(f"  Expires in: {token_response.get('expires_in', 'N/A')} seconds")
            print(f"  Scope: {token_response.get('scope', 'N/A')}")
            print(f"  Token (first 50 chars): {access_token[:50]}...")
            
            # Decode and display JWT payload
            self._decode_jwt_payload(access_token)
            
            return access_token
            
        except requests.exceptions.RequestException as e:
            print(f"\n✗ Failed to obtain token: {e}")
            if hasattr(e, 'response') and e.response is not None:
                print(f"Response: {e.response.text}")
            raise
    
    def _decode_jwt_payload(self, token):
        """Decode and display JWT payload"""
        try:
            parts = token.split('.')
            if len(parts) != 3:
                return
            
            payload_b64 = parts[1]
            padding = 4 - len(payload_b64) % 4
            if padding != 4:
                payload_b64 += '=' * padding
            
            payload_json = base64.urlsafe_b64decode(payload_b64).decode('utf-8')
            payload = json.loads(payload_json)
            
            print(f"\n  JWT Payload (decoded):")
            print(f"    Issuer (iss): {payload.get('iss', 'N/A')}")
            print(f"    Subject (sub): {payload.get('sub', 'N/A')}")
            print(f"    Preferred Username: {payload.get('preferred_username', 'N/A')}")
            print(f"    Email: {payload.get('email', 'N/A')}")
            print(f"    Audience (aud): {payload.get('aud', 'N/A')}")
            print(f"    Issued at: {payload.get('iat', 'N/A')}")
            print(f"    Expires at: {payload.get('exp', 'N/A')}")
            
        except Exception as e:
            print(f"  (Could not decode JWT payload: {e})")
    
    def send_email_with_xoauth2(self, access_token):
        """Authenticate with XOAUTH2 and send an email"""
        print("\n" + "="*70)
        print("STEP 2: Authenticating with XOAUTH2 (Token Will Be Validated!)")
        print("="*70)
        
        # XOAUTH2 format: user=<username>\x01auth=Bearer <token>\x01\x01
        # The format requires 4 parts separated by \x01
        auth_string = f"user={self.username}\x01auth=Bearer {access_token}\x01\x01"
        auth_bytes = auth_string.encode('utf-8')
        auth_b64 = base64.b64encode(auth_bytes).decode('utf-8')
        
        print(f"SMTP server: {self.smtp_host}:{self.smtp_port}")
        print(f"Auth mechanism: XOAUTH2")
        print(f"Username: {self.username}")
        print(f"\nsmtp4dev will now:")
        print(f"  1. Receive the XOAUTH2 authentication")
        print(f"  2. Extract the JWT token")
        print(f"  3. Fetch Keycloak's public keys (JWKS)")
        print(f"  4. Validate token signature")
        print(f"  5. Verify token hasn't expired")
        print(f"  6. Check issuer matches Keycloak")
        print(f"  7. Extract subject claim")
        print(f"  8. Match subject with username")
        print(f"  9. Verify username is in Users list")
        
        try:
            print(f"\nConnecting to SMTP server...")
            smtp = smtplib.SMTP(self.smtp_host, self.smtp_port, timeout=10)
            smtp.set_debuglevel(0)
            
            smtp.ehlo()
            
            print(f"\nSending AUTH XOAUTH2 command...")
            code, msg = smtp.docmd('AUTH', 'XOAUTH2')
            
            if code == 334:
                print(f"Server ready for credentials, sending token...")
                code, msg = smtp.docmd(auth_b64)
                
                if code == 235:
                    print(f"\n" + "="*70)
                    print("✓ SUCCESS: OAuth2 Token Validated by smtp4dev!")
                    print("="*70)
                    print(f"  This means:")
                    print(f"  ✓ Token signature was valid")
                    print(f"  ✓ Token hasn't expired")
                    print(f"  ✓ Issuer matched Keycloak")
                    print(f"  ✓ Subject matched username")
                    print(f"  ✓ Username found in configured users")
                    print(f"\n  Server response: {code} {msg.decode('utf-8', errors='ignore')}")
                    
                    # Send email
                    print(f"\n" + "="*70)
                    print("STEP 3: Sending Test Email")
                    print("="*70)
                    
                    msg_obj = MIMEMultipart()
                    msg_obj['From'] = f"{self.username}@example.com"
                    msg_obj['To'] = "recipient@test.local"
                    msg_obj['Subject'] = "✓ End-to-End OAuth2/XOAUTH2 Test SUCCESSFUL!"
                    
                    body = f"""
SUCCESS! This email proves OAuth2/XOAUTH2 authentication is working end-to-end!

═══════════════════════════════════════════════════════════════════════

What Was Tested:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✓ OAuth2 Token Acquisition from Keycloak
✓ JWT Token Validation (signature, expiration, issuer)
✓ Subject Claim Extraction and Matching
✓ Username Verification Against Configured Users List
✓ XOAUTH2 SMTP Authentication
✓ Email Delivery

Authentication Flow:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
1. Client obtained access token from Keycloak (OAuth2/OIDC provider)
2. Client connected to smtp4dev SMTP server
3. Client sent AUTH XOAUTH2 command
4. Client sent username + JWT token
5. smtp4dev fetched Keycloak's public signing keys (JWKS)
6. smtp4dev validated token signature using public keys
7. smtp4dev verified token expiration
8. smtp4dev validated issuer claim
9. smtp4dev extracted subject claim: "{self.username}"
10. smtp4dev matched subject with XOAUTH2 username
11. smtp4dev verified username in configured users list
12. Authentication SUCCESSFUL!

Components:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
• Identity Provider: Keycloak
• Realm: {self.realm}
• Client ID: {self.client_id}
• Username: {self.username}
• SMTP Server: smtp4dev with OAuth2 validation enabled
• Auth Mechanism: XOAUTH2

Timestamp: {time.strftime('%Y-%m-%d %H:%M:%S UTC', time.gmtime())}

═══════════════════════════════════════════════════════════════════════
"""
                    msg_obj.attach(MIMEText(body, 'plain'))
                    
                    smtp.send_message(msg_obj)
                    print(f"✓ Email sent successfully!")
                    
                    smtp.quit()
                    return True
                else:
                    print(f"\n✗ Authentication FAILED!")
                    print(f"  Server response: {code} {msg.decode('utf-8', errors='ignore')}")
                    print(f"\n  Possible reasons:")
                    print(f"  - Token validation failed")
                    print(f"  - Subject doesn't match username")
                    print(f"  - Username not in configured users")
                    smtp.quit()
                    return False
            else:
                print(f"✗ Unexpected response: {code} {msg.decode('utf-8', errors='ignore')}")
                smtp.quit()
                return False
                
        except Exception as e:
            print(f"\n✗ Failed: {e}")
            import traceback
            traceback.print_exc()
            return False
    
    def verify_email_received(self):
        """Verify the email was received"""
        print("\n" + "="*70)
        print("STEP 4: Verifying Email Reception")
        print("="*70)
        
        try:
            time.sleep(2)
            response = requests.get(f"{self.api_url}/messages", timeout=10)
            response.raise_for_status()
            
            messages = response.json()
            
            if not messages:
                print("✗ No messages found")
                return False
            
            print(f"✓ Found {len(messages)} message(s)")
            
            for msg in messages:
                if "End-to-End OAuth2/XOAUTH2" in msg.get('subject', ''):
                    print(f"\n  Message Details:")
                    print(f"    ID: {msg.get('id')}")
                    print(f"    From: {msg.get('from')}")
                    print(f"    To: {msg.get('to')}")
                    print(f"    Subject: {msg.get('subject')}")
                    return True
            
            return False
            
        except Exception as e:
            print(f"✗ Failed to verify: {e}")
            return False
    
    def run(self):
        """Run the complete end-to-end test"""
        print("\n" + "="*70)
        print("END-TO-END OAuth2/XOAUTH2 AUTHENTICATION TEST")
        print("with Keycloak Identity Provider")
        print("="*70)
        
        try:
            # Step 1: Get OAuth2 token from Keycloak
            access_token = self.get_oauth2_token()
            
            # Step 2: Authenticate and send email
            if not self.send_email_with_xoauth2(access_token):
                print("\n" + "="*70)
                print("✗ TEST FAILED")
                print("="*70)
                return False
            
            # Step 3: Verify email
            if not self.verify_email_received():
                print("\n" + "="*70)
                print("✗ TEST FAILED - Email not received")
                print("="*70)
                return False
            
            print("\n" + "="*70)
            print("✓✓✓ END-TO-END TEST SUCCESSFUL! ✓✓✓")
            print("="*70)
            print(f"\nWhat was demonstrated:")
            print(f"  ✓ Real OAuth2 token from Keycloak")
            print(f"  ✓ JWT token validation (signature + claims)")
            print(f"  ✓ XOAUTH2 SMTP authentication")
            print(f"  ✓ User authorization via Users list")
            print(f"  ✓ Email delivery")
            print(f"\nView the email:")
            print(f"  http://localhost:5000")
            print(f"\nKeycloak admin:")
            print(f"  http://localhost:8080")
            print(f"  Username: admin")
            print(f"  Password: admin")
            print("\n" + "="*70)
            
            return True
            
        except Exception as e:
            print(f"\n✗ Test failed with error: {e}")
            import traceback
            traceback.print_exc()
            return False


if __name__ == "__main__":
    test = OAuth2E2ETest()
    success = test.run()
    sys.exit(0 if success else 1)
