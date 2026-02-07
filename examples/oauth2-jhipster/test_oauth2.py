#!/usr/bin/env python3
"""
OAuth2/XOAUTH2 Authentication Test Script for smtp4dev with JHipster Registry

This script demonstrates end-to-end OAuth2 authentication:
1. Obtains an access token from JHipster Registry
2. Authenticates to smtp4dev using XOAUTH2
3. Sends a test email
4. Verifies the email was received
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
    print("Error: 'requests' library not found. Install it with: pip3 install requests")
    sys.exit(1)


class OAuth2XOauth2Demo:
    """Demonstrates OAuth2/XOAUTH2 authentication with smtp4dev"""
    
    def __init__(self):
        # JHipster Registry configuration
        self.token_url = "http://localhost:8761/oauth/token"
        self.client_id = "internal"
        self.client_secret = "internal"
        self.username = "admin"
        self.password = "admin"
        
        # smtp4dev configuration
        self.smtp_host = "localhost"
        self.smtp_port = 2525
        self.api_url = "http://localhost:5000/api"
        
    def get_oauth2_token(self):
        """Obtain an OAuth2 access token from JHipster Registry"""
        print("\n" + "="*70)
        print("STEP 1: Obtaining OAuth2 Access Token")
        print("="*70)
        
        # Prepare Basic Auth header
        auth_string = f"{self.client_id}:{self.client_secret}"
        auth_bytes = auth_string.encode('utf-8')
        auth_b64 = base64.b64encode(auth_bytes).decode('utf-8')
        
        headers = {
            'Authorization': f'Basic {auth_b64}',
            'Content-Type': 'application/x-www-form-urlencoded'
        }
        
        data = {
            'grant_type': 'password',
            'username': self.username,
            'password': self.password,
            'scope': 'read write'
        }
        
        print(f"Token endpoint: {self.token_url}")
        print(f"Client ID: {self.client_id}")
        print(f"Username: {self.username}")
        print(f"Grant type: password")
        
        try:
            response = requests.post(self.token_url, headers=headers, data=data, timeout=10)
            response.raise_for_status()
            
            token_response = response.json()
            access_token = token_response['access_token']
            
            print(f"\n✓ Token obtained successfully!")
            print(f"  Token type: {token_response.get('token_type', 'N/A')}")
            print(f"  Expires in: {token_response.get('expires_in', 'N/A')} seconds")
            print(f"  Scope: {token_response.get('scope', 'N/A')}")
            print(f"  Token (first 50 chars): {access_token[:50]}...")
            
            # Decode and display JWT payload (for demonstration)
            self._decode_jwt_payload(access_token)
            
            return access_token
            
        except requests.exceptions.RequestException as e:
            print(f"\n✗ Failed to obtain token: {e}")
            raise
    
    def _decode_jwt_payload(self, token):
        """Decode and display JWT payload (without verification)"""
        try:
            # JWT format: header.payload.signature
            parts = token.split('.')
            if len(parts) != 3:
                return
            
            # Decode payload (add padding if needed)
            payload_b64 = parts[1]
            # Add padding
            padding = 4 - len(payload_b64) % 4
            if padding != 4:
                payload_b64 += '=' * padding
            
            payload_json = base64.urlsafe_b64decode(payload_b64).decode('utf-8')
            payload = json.loads(payload_json)
            
            print(f"\n  JWT Payload:")
            print(f"    Issuer (iss): {payload.get('iss', 'N/A')}")
            print(f"    Subject (sub): {payload.get('sub', 'N/A')}")
            print(f"    Audience (aud): {payload.get('aud', 'N/A')}")
            print(f"    Issued at: {payload.get('iat', 'N/A')}")
            print(f"    Expires at: {payload.get('exp', 'N/A')}")
            
        except Exception as e:
            print(f"  (Could not decode JWT payload: {e})")
    
    def send_email_with_xoauth2(self, access_token):
        """Authenticate with XOAUTH2 and send an email"""
        print("\n" + "="*70)
        print("STEP 2: Authenticating with XOAUTH2 and Sending Email")
        print("="*70)
        
        # Prepare XOAUTH2 auth string
        # Format: user=<username>\x01auth=Bearer <token>\x01\x01
        auth_string = f"user={self.username}\x01auth=Bearer {access_token}\x01\x01"
        auth_bytes = auth_string.encode('utf-8')
        auth_b64 = base64.b64encode(auth_bytes).decode('utf-8')
        
        print(f"SMTP server: {self.smtp_host}:{self.smtp_port}")
        print(f"Auth mechanism: XOAUTH2")
        print(f"Username: {self.username}")
        print(f"Auth string (first 100 chars): {auth_string[:100].replace(chr(1), '\\x01')}...")
        
        try:
            # Connect to SMTP server
            print(f"\nConnecting to SMTP server...")
            smtp = smtplib.SMTP(self.smtp_host, self.smtp_port, timeout=10)
            smtp.set_debuglevel(0)  # Set to 1 to see SMTP conversation
            
            # Send EHLO
            smtp.ehlo()
            
            # Authenticate with XOAUTH2
            print(f"Authenticating with XOAUTH2...")
            
            # SMTP AUTH XOAUTH2 mechanism
            # Send 'AUTH XOAUTH2' command
            code, msg = smtp.docmd('AUTH', 'XOAUTH2')
            
            if code == 334:  # Server expects auth data
                # Send the base64-encoded auth string
                code, msg = smtp.docmd(auth_b64)
                
                if code == 235:  # Authentication successful
                    print(f"✓ Authentication successful!")
                else:
                    print(f"✗ Authentication failed: {code} {msg.decode('utf-8', errors='ignore')}")
                    smtp.quit()
                    return False
            else:
                print(f"✗ Unexpected response to AUTH XOAUTH2: {code} {msg.decode('utf-8', errors='ignore')}")
                smtp.quit()
                return False
            
            # Create and send email
            print(f"\nSending test email...")
            msg = MIMEMultipart()
            msg['From'] = f"{self.username}@test.local"
            msg['To'] = "recipient@test.local"
            msg['Subject'] = "OAuth2/XOAUTH2 Test Email"
            
            body = f"""
This is a test email sent using OAuth2/XOAUTH2 authentication.

Authentication Details:
- Identity Provider: JHipster Registry
- Auth Mechanism: XOAUTH2
- Username: {self.username}
- Token validated: Yes
- Timestamp: {time.strftime('%Y-%m-%d %H:%M:%S')}

This demonstrates that smtp4dev successfully validated the OAuth2 token
against the JHipster Registry IDP.
"""
            msg.attach(MIMEText(body, 'plain'))
            
            smtp.send_message(msg)
            print(f"✓ Email sent successfully!")
            
            smtp.quit()
            return True
            
        except Exception as e:
            print(f"\n✗ Failed to send email: {e}")
            import traceback
            traceback.print_exc()
            return False
    
    def verify_email_received(self):
        """Verify the email was received via smtp4dev API"""
        print("\n" + "="*70)
        print("STEP 3: Verifying Email Reception")
        print("="*70)
        
        print(f"Checking smtp4dev API: {self.api_url}/messages")
        
        try:
            # Wait a moment for email to be processed
            time.sleep(2)
            
            response = requests.get(f"{self.api_url}/messages", timeout=10)
            response.raise_for_status()
            
            messages = response.json()
            
            if not messages:
                print("✗ No messages found in smtp4dev")
                return False
            
            print(f"\n✓ Found {len(messages)} message(s) in smtp4dev")
            
            # Find our test message
            for msg in messages:
                if "OAuth2/XOAUTH2 Test Email" in msg.get('subject', ''):
                    print(f"\n✓ Test email found!")
                    print(f"  Message ID: {msg.get('id')}")
                    print(f"  From: {msg.get('from')}")
                    print(f"  To: {msg.get('to')}")
                    print(f"  Subject: {msg.get('subject')}")
                    print(f"  Received: {msg.get('receivedDate')}")
                    return True
            
            print("✗ Test email not found (but other messages exist)")
            return False
            
        except Exception as e:
            print(f"\n✗ Failed to verify email: {e}")
            return False
    
    def run(self):
        """Run the complete demo"""
        print("\n" + "="*70)
        print("OAuth2/XOAUTH2 Authentication Demo")
        print("smtp4dev with JHipster Registry")
        print("="*70)
        
        try:
            # Step 1: Get OAuth2 token
            access_token = self.get_oauth2_token()
            
            # Step 2: Authenticate and send email
            if not self.send_email_with_xoauth2(access_token):
                print("\n✗ Demo failed at email sending step")
                return False
            
            # Step 3: Verify email was received
            if not self.verify_email_received():
                print("\n✗ Demo failed at verification step")
                return False
            
            print("\n" + "="*70)
            print("✓ Demo completed successfully!")
            print("="*70)
            print("\nYou can view the email in the smtp4dev web UI:")
            print(f"  http://localhost:5000")
            print("\nYou can access JHipster Registry at:")
            print(f"  http://localhost:8761 (login: admin/admin)")
            print("\n")
            
            return True
            
        except Exception as e:
            print(f"\n✗ Demo failed with error: {e}")
            import traceback
            traceback.print_exc()
            return False


def main():
    """Main entry point"""
    demo = OAuth2XOauth2Demo()
    success = demo.run()
    sys.exit(0 if success else 1)


if __name__ == "__main__":
    main()
