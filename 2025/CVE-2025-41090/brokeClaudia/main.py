from InquirerPy import prompt
from core.modules.artascii.artascii_draw import ArtAsciiDraw
from core.modules.historybrowser.historybrowser_obtain import HistoryBrowserObtain
from core.modules.microclaudia.microclaudia_pocs import MicroClaudiaPoCs
from core.modules.microclaudia.microclaudia_signin import MicroClaudiaSignIn


# Main
def main():
    
    try:

        # Banner
        art_ascii_draw = ArtAsciiDraw()
        art_ascii_draw.banner()

        # Options
        option_api_endpoints = 'microCLAUDIA API Endpoints'
        option_sign_in = 'Sign In to microCLAUDIA Website'
        option_poc_users = 'PoC - Broken Access Control (Check with two accounts)'
        option_poc_request = 'PoC - Broken Access Control (Request private element)'
        option_browser_history = 'Obtain Secret Identifiers (Extract History)'
        option_exit = 'Exit'

        # Question
        question = {
            "type": "list",
            "message": "Select an action:",
            "choices": [option_api_endpoints, option_sign_in, option_poc_users, option_poc_request, option_browser_history, option_exit]
        }

        # Classes
        microclaudia_signin = MicroClaudiaSignIn()
        microclaudia_pocs = MicroClaudiaPoCs()
        history_browser_obtain = HistoryBrowserObtain()

        # Loop
        while True:
            result = prompt(questions=question)

            # Menu
            if result[0] == option_api_endpoints:
                microclaudia_signin.table_endpoints()
            elif result[0] == option_sign_in:
                microclaudia_signin.signin_website()
            elif result[0] == option_poc_users:
                microclaudia_pocs.broken_access_control_users()
            elif result[0] == option_poc_request:
                microclaudia_pocs.broken_access_control_request()
            elif result[0] == option_browser_history:
                history_browser_obtain.browsers()
            elif result[0] == option_exit:
                exit(0)
            else:
                print('Invalid option. Please choose a valid action.')

    except Exception as e:
        print(e)


if __name__ == "__main__":
    main()
