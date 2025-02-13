import tkinter as tk
from PIL import Image, ImageTk

# Chemin de ton image (remplace par le vrai chemin si besoin)
image_path = "/img/spoor.png"  # Mets ici ton image de voie ferr�e

# Cr�ation de la fen�tre principale
root = tk.Tk()
root.title("Affichage de la voie ferr�e")

# Charger l'image
image = Image.open(image_path)
image = image.resize((800, 400))  # Ajuste la taille si besoin
photo = ImageTk.PhotoImage(image)

# Cr�er un Canvas pour afficher l'image
canvas = tk.Canvas(root, width=800, height=400)
canvas.pack()

# Ajouter l'image dans le canvas
canvas.create_image(0, 0, anchor=tk.NW, image=photo)

# Lancer l'interface Tkinter
root.mainloop()
